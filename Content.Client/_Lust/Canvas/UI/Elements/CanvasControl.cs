using System.IO;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Client.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client._Lust.Canvas.UI.Elements;

/// <summary>
/// Поле рисования холста
/// </summary>
public sealed partial class CanvasControl : Control
{
    [Dependency] private IClyde _clyde = default!;

    public const int CanvasSize = 256;

    private readonly CanvasToolState _tools;
    private Image<Rgba32> _buffer;
    private readonly float[] _strokeAlpha = new float[CanvasSize * CanvasSize];
    private readonly OwnedTexture _texture;
    private readonly List<Image<Rgba32>> _undo = [];
    private bool _drawing;
    private Vector2 _lastCell;
    private bool _hasLastCell;
    private Vector2 _lastStamp;
    private Vector2 _hoverPos;
    private bool _hovering;

    private static readonly Rgba32 CanvasWhite = new(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);

    public CanvasControl(CanvasToolState tools)
    {
        IoCManager.InjectDependencies(this);
        _tools = tools;

        _buffer = new Image<Rgba32>(CanvasSize, CanvasSize);

        var white = new Rgba32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);

        for (var y = 0; y < CanvasSize; y++)
        for (var x = 0; x < CanvasSize; x++)
        {
            _buffer[x, y] = white;
        }

        _texture = _clyde.CreateBlankTexture<Rgba32>((CanvasSize, CanvasSize));
        _texture.SetSubImage(Vector2i.Zero, _buffer);

        MinSize = new Vector2(512, 512);
        MouseFilter = MouseFilterMode.Stop;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        var rect = CanvasRect();
        handle.DrawTextureRect(_texture, rect);

        if (!_hovering || _tools.Tool is not (CanvasTool.Pen or CanvasTool.Brush or CanvasTool.Eraser))
            return;

        var radius = Math.Max(1, _tools.Size / 2f) * rect.Size.X / CanvasSize;
        handle.DrawCircle(_hoverPos, radius + 1f, Color.White, filled: false);
        handle.DrawCircle(_hoverPos, radius, Color.Black, filled: false);
    }

    /// <summary>
    /// Квадратная область холста по центру контрола.
    /// </summary>
    private UIBox2 CanvasRect()
    {
        var side = Math.Min(Size.X, Size.Y);
        var offset = (Size - new Vector2(side, side)) / 2f;
        return UIBox2.FromDimensions(offset, new Vector2(side, side));
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        if (!TryGetCell(args.RelativePosition, out var px, out var py))
            return;

        // выбор одноразовых инструментов
        switch (_tools.Tool)
        {
            case CanvasTool.Eyedropper:
                PickColor(px, py);
                args.Handle();
                return;

            case CanvasTool.Fill:
                PushUndo();
                FloodFill(px, py);
                _texture.SetSubImage(Vector2i.Zero, _buffer);
                args.Handle();
                return;
        }

        // Ластик карандаш кисть
        PushUndo();
        Array.Clear(_strokeAlpha);
        _drawing = true;
        _hasLastCell = false;
        PaintStroke(args.RelativePosition);
        args.Handle();
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        _drawing = false;
        _hasLastCell = false;
        args.Handle();
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        _hoverPos = args.RelativePosition;
        _hovering = true;

        if (!_drawing)
            return;

        PaintStroke(args.RelativePosition);
        args.Handle();
    }

    protected override void MouseExited()
    {
        base.MouseExited();
        _hovering = false;
    }

    /// <summary>
    /// Настройка штампов
    /// </summary>
    private readonly struct BrushStyle
    {
        /// <summary>Цвет, который кладём на холст</summary>
        public required Rgba32 Color { get; init; }

        /// <summary>Непрозрачность из ползунка</summary>
        public required float Alpha { get; init; }

        /// <summary>Мягкость краев.</summary>
        public required bool Soft { get; init; }
    }

    /// <summary>
    /// Функция штриха по позициям мышки
    /// </summary>
    private void PaintStroke(Vector2 relative)
    {
        var p = ToCanvasPos(relative);

        if (_hasLastCell)
        {
            PaintClippedLine(_lastCell, p);
        }
        else if (p.X >= 0f && p.Y >= 0f && p.X < CanvasSize && p.Y < CanvasSize)
        {
            StampBrush((int) p.X, (int) p.Y);
            _lastStamp = p;
        }

        _lastCell = p;
        _hasLastCell = true;
        _texture.SetSubImage(Vector2i.Zero, _buffer);
    }

    /// <summary>
    /// Экранные координаты в плавающую позицию на холсте. Может лежать вне границ.
    /// </summary>
    private Vector2 ToCanvasPos(Vector2 relative)
    {
        var rect = CanvasRect();
        return new Vector2(
            (relative.X - rect.Left) / rect.Size.X * CanvasSize,
            (relative.Y - rect.Top) / rect.Size.Y * CanvasSize);
    }

    /// <summary>
    /// Ставит штампы вдоль отрезка, отсекая его границей холста (Лян-Барски).
    /// </summary>
    private void PaintClippedLine(Vector2 from, Vector2 to)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var t0 = 0f;
        var t1 = 1f;

        if (!Clip(-dx, from.X, ref t0, ref t1)
            || !Clip(dx, CanvasSize - from.X, ref t0, ref t1)
            || !Clip(-dy, from.Y, ref t0, ref t1)
            || !Clip(dy, CanvasSize - from.Y, ref t0, ref t1))
            return;

        var radius = Math.Max(1, _tools.Size / 2);
        var spacing = Math.Max(1, radius / 4f);
        var steps = Math.Max(1, (int) Math.Ceiling((to - from).Length()));

        for (var i = 0; i <= steps; i++)
        {
            var t = t0 + (t1 - t0) * i / steps;
            var pt = new Vector2(from.X + dx * t, from.Y + dy * t);

            if ((pt - _lastStamp).Length() < spacing)
                continue;

            StampBrush((int) MathF.Round(pt.X), (int) MathF.Round(pt.Y));
            _lastStamp = pt;
        }
    }

    /// <summary>
    /// Проверка не вышел ли за граници хоста
    /// </summary>
    private static bool Clip(float direction, float distance, ref float t0, ref float t1)
    {
        if (direction == 0f)
            return distance >= 0f;

        var r = distance / direction;
        if (direction < 0f)
        {
            if (r > t1)
                return false;
            if (r > t0)
                t0 = r;
        }
        else
        {
            if (r < t0)
                return false;
            if (r < t1)
                t1 = r;
        }

        return true;
    }

    /// <summary>
    /// Константа размера прозрачного края кисти
    /// </summary>
    private const float CoreFraction = 0.65f;

    /// <summary>
    /// Создания штампа кисти
    /// </summary>
    private void StampBrush(int px, int py)
    {
        var style = GetStyle();
        var size = Math.Max(1, _tools.Size);
        var half = size / 2;
        var radius = size / 2f;

        var offset = size % 2 == 0 ? 0.5f : 0f;

        var basePixels = (_undo.Count > 0 ? _undo[^1] : _buffer).GetPixelSpan();
        var pixels = _buffer.GetPixelSpan();

        for (var y = py - half; y < py - half + size; y++)
        {
            if (y < 0 || y >= CanvasSize)
                continue;

            for (var x = px - half; x < px - half + size; x++)
            {
                if (x < 0 || x >= CanvasSize)
                    continue;

                var ox = MathF.Abs(x - px + offset) / radius;
                var oy = MathF.Abs(y - py + offset) / radius;
                var dist = MathF.Sqrt(ox * ox + oy * oy);

                if (dist > 1f)
                    continue;

                var coverage = style.Soft
                    ? 1f - Smoothstep(Math.Clamp((dist - CoreFraction) / (1f - CoreFraction), 0f, 1f))
                    : 1f;

                var stampAlpha = style.Alpha * coverage;
                var index = y * CanvasSize + x;

                if (stampAlpha <= _strokeAlpha[index])
                    continue;
                _strokeAlpha[index] = stampAlpha;

                var baseColor = basePixels[index];
                pixels[index] = new Rgba32(
                    Blend(style.Color.R, baseColor.R, stampAlpha),
                    Blend(style.Color.G, baseColor.G, stampAlpha),
                    Blend(style.Color.B, baseColor.B, stampAlpha),
                    byte.MaxValue);
            }
        }
    }

    /// <summary>
    /// Функция получение настройки инструмента
    /// </summary>
    private BrushStyle GetStyle()
    {
        if (_tools.Tool == CanvasTool.Eraser)
        {
            return new BrushStyle
            {
                Color = new Rgba32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue),
                Alpha = 1f,
                Soft = false,
            };
        }

        var c = _tools.BrushColor;
        return new BrushStyle
        {
            Color = new Rgba32(c.RByte, c.GByte, c.BByte, byte.MaxValue),
            Alpha = c.A,
            Soft = _tools.Tool == CanvasTool.Brush,
        };
    }

    private static byte Blend(byte source, byte destination, float alpha)
    {
        return (byte) (source * alpha + destination * (1f - alpha));
    }

    /// <summary>
    /// Плавность  прозрачности кисти
    /// </summary>
    private static float Smoothstep(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return t * t * (3f - 2f * t);
    }
    /// <summary>
    /// Пипетка
    /// </summary>
    /// <param name="px"></param>
    /// <param name="py"></param>
    private void PickColor(int px, int py)
    {
        var pixels = _buffer.GetPixelSpan();
        var pixel = pixels[py * CanvasSize + px];
        _tools.SetExternal(new Color(pixel.R, pixel.G, pixel.B));
    }

    /// <summary>
    /// Порог разности цветов для заливки
    /// </summary>
    private const int FillTolerance = 36;

    /// <summary>
    /// Заливка
    /// </summary>
    private void FloodFill(int sx, int sy)
    {
        var pixels = _buffer.GetPixelSpan();
        var startIndex = sy * CanvasSize + sx;
        var start = pixels[startIndex];

        var c = _tools.Color;
        var fill = new Rgba32(c.RByte, c.GByte, c.BByte, byte.MaxValue);

        if (start == fill)
            return;

        var toleranceSq = FillTolerance * FillTolerance;

        var pending = new Stack<Vector2i>();
        pending.Push(new Vector2i(sx, sy));

        while (pending.Count > 0)
        {
            var seed = pending.Pop();
            var y = seed.Y;
            var row = y * CanvasSize;
            var left = seed.X;

            while (left > 0 && Matches(pixels[row + left - 1], start, toleranceSq))
            {
                left--;
            }

            var spanUp = false;
            var spanDown = false;

            for (var x = left; x < CanvasSize && Matches(pixels[row + x], start, toleranceSq); x++)
            {
                pixels[row + x] = fill;

                if (y > 0)
                {
                    var up = Matches(pixels[row - CanvasSize + x], start, toleranceSq);
                    if (up && !spanUp)
                    {
                        pending.Push(new Vector2i(x, y - 1));
                        spanUp = true;
                    }
                    else if (!up)
                    {
                        spanUp = false;
                    }
                }

                if (y < CanvasSize - 1)
                {
                    var down = Matches(pixels[row + CanvasSize + x], start, toleranceSq);
                    if (down && !spanDown)
                    {
                        pending.Push(new Vector2i(x, y + 1));
                        spanDown = true;
                    }
                    else if (!down)
                    {
                        spanDown = false;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Сравнение цветов
    /// Цвет считается тем же если квадрат разности каналов в пределах допуска.
    /// </summary>
    private static bool Matches(Rgba32 current, Rgba32 target, int toleranceSq)
    {
        var dr = current.R - target.R;
        var dg = current.G - target.G;
        var db = current.B - target.B;
        return dr * dr + dg * dg + db * db <= toleranceSq;
    }

    /// <summary>
    /// Снимок текущего холста перед изменением, чтобы можно было откатить.
    /// </summary>
    private void PushUndo()
    {
        _undo.Add(_buffer.Clone());

        if (_undo.Count <= UndoLimit)
            return;

        _undo[0].Dispose();
        _undo.RemoveAt(0);
    }

    private const int UndoLimit = 20;

    /// <summary>
    /// Откат последнего изменения.
    /// </summary>
    public void Undo()
    {
        if (_undo.Count == 0)
            return;

        _buffer.Dispose();
        _buffer = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        _texture.SetSubImage(Vector2i.Zero, _buffer);
    }

    /// <summary>
    /// Серелизация картинки png
    /// </summary>
    public byte[]? Save()
    {
        var pixels = _buffer.GetPixelSpan();
        var empty = true;
        for (var i = 0; i < pixels.Length; i++)
        {
            if (pixels[i] != CanvasWhite)
            {
                empty = false;
                break;
            }
        }

        if (empty)
            return null;

        using var stream = new MemoryStream();
        _buffer.SaveAsPng(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Экранные координаты мыши в пиксель холста возвращает false если курсор вне холста.
    /// </summary>
    private bool TryGetCell(Vector2 relative, out int x, out int y)
    {
        var rect = CanvasRect();

        var localX = (relative.X - rect.Left) / rect.Size.X;
        var localY = (relative.Y - rect.Top) / rect.Size.Y;

        if (localX < 0f || localY < 0f || localX >= 1f || localY >= 1f)
        {
            x = y = 0;
            return false;
        }

        x = Math.Clamp((int) (localX * CanvasSize), 0, CanvasSize - 1);
        y = Math.Clamp((int) (localY * CanvasSize), 0, CanvasSize - 1);
        return true;
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();
        _texture.Dispose();
        _buffer.Dispose();

        foreach (var snap in _undo)
        {
            snap.Dispose();
        }

        _undo.Clear();
    }
}
