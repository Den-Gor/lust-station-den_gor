using System.Linq;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client._Lust.Canvas;

/// <summary>
/// Пиксельный холст 256x256. Буфер правится на CPU, на GPU уходит через SetSubImage.
/// Инструмент/цвет/размер читаются из общего <see cref="CanvasToolState"/>.
/// </summary>
public sealed partial class CanvasControl : Control
{
    [Dependency] private IClyde _clyde = default!;

    public const int CanvasSize = 256;

    private readonly CanvasToolState _tools;
    private Image<Rgba32> _buffer;
    private byte[] _pixels = new byte[CanvasSize * CanvasSize * 4];
    private readonly float[] _strokeAlpha = new float[CanvasSize * CanvasSize];
    private readonly OwnedTexture _texture;
    private readonly Stack<(Image<Rgba32> Image, byte[] Pixels)> _undo = new();
    private bool _drawing;
    private Vector2 _lastCell;
    private bool _hasLastCell;
    private Vector2 _lastStamp;
    private Vector2 _hoverPos;
    private bool _hovering;

    public CanvasControl(CanvasToolState tools)
    {
        IoCManager.InjectDependencies(this);
        _tools = tools;

        _buffer = new Image<Rgba32>(CanvasSize, CanvasSize);

        var white = new Rgba32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
        for (var i = 0; i < _pixels.Length; i++)
        {
            _pixels[i] = byte.MaxValue;
        }

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

        // Кольцо текущего размера кисти
        if (_hovering && _tools.Tool is CanvasTool.Pen or CanvasTool.Brush or CanvasTool.Eraser)
        {
            var radius = Math.Max(1, _tools.Size / 2) * rect.Size.X / CanvasSize;
            handle.DrawCircle(_hoverPos, radius + 1f, Color.White, filled: false);
            handle.DrawCircle(_hoverPos, radius, Color.Black, filled: false);
        }
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

        if (_tools.Tool == CanvasTool.Eyedropper)
        {
            PickColor(args.RelativePosition);
            args.Handle();
            return;
        }

        PushUndo();
        Array.Clear(_strokeAlpha, 0, _strokeAlpha.Length);
        _drawing = true;
        PaintAt(args.RelativePosition);
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
        _hoverPos = args.RelativePosition;
        _hovering = true;
        if (!_drawing)
            return;
        PaintAt(args.RelativePosition);
        args.Handle();
    }

    protected override void MouseExited()
    {
        _hovering = false;
    }

    private void PaintAt(Vector2 relative)
    {
        var rect = CanvasRect();
        var p = new Vector2(
            (relative.X - rect.Left) / rect.Size.X * (CanvasSize - 1),
            (relative.Y - rect.Top) / rect.Size.Y * (CanvasSize - 1));

        if (_tools.Tool == CanvasTool.Fill)
        {
            if (!_hasLastCell && Inside(p))
                FloodFill((int) p.X, (int) p.Y);
            _lastCell = p;
            _hasLastCell = true;
            _texture.SetSubImage(Vector2i.Zero, _buffer);
            return;
        }

        if (_hasLastCell)
            PaintClipped(_lastCell, p);
        else if (Inside(p))
        {
            PaintCircle((int) p.X, (int) p.Y);
            _lastStamp = p;
        }

        _lastCell = p;
        _hasLastCell = true;

        _texture.SetSubImage(Vector2i.Zero, _buffer);
    }

    private static bool Inside(Vector2 p)
    {
        return p.X >= 0f && p.Y >= 0f && p.X < CanvasSize && p.Y < CanvasSize;
    }

    /// <summary>
    /// Линия штампов между точками, отсеченная границей холста (Лян-Барски).
    /// </summary>
    private void PaintClipped(Vector2 from, Vector2 to)
    {
        const float min = 0f;
        const float max = CanvasSize - 1;

        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var t0 = 0f;
        var t1 = 1f;

        if (!Clip(-dx, from.X - min, ref t0, ref t1)
            || !Clip(dx, max - from.X, ref t0, ref t1)
            || !Clip(-dy, from.Y - min, ref t0, ref t1)
            || !Clip(dy, max - from.Y, ref t0, ref t1))
            return;

        var radius = Math.Max(1, _tools.Size / 2);
        var spacing = Math.Max(1, radius / 4);
        var steps = Math.Max(1, (int) Math.Ceiling((to - from).Length()));
        for (var i = 0; i <= steps; i++)
        {
            var t = t0 + (t1 - t0) * i / steps;
            var pt = new Vector2(from.X + dx * t, from.Y + dy * t);
            if ((pt - _lastStamp).Length() < spacing)
                continue;
            PaintCircle((int) Math.Round(pt.X), (int) Math.Round(pt.Y));
            _lastStamp = pt;
        }
    }

    private static bool Clip(float p, float q, ref float t0, ref float t1)
    {
        if (p == 0f)
            return q >= 0f;
        var r = q / p;
        if (p < 0f)
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
    /// Один штамп инструмента в пикселе. Форма и база бленда зависят от инструмента.
    /// </summary>
    private void PaintCircle(int px, int py)
    {
        var eraser = _tools.Tool == CanvasTool.Eraser;
        var soft = _tools.Tool == CanvasTool.Brush;
        var pen = _tools.Tool == CanvasTool.Pen;
        var size = Math.Max(1, _tools.Size);
        var half = size / 2;

        var c = eraser ? Color.White : _tools.Color;
        var alpha = eraser ? 1f : c.AByte / 255f;

        var hr = size / 2f;
        var even = size % 2 == 0;

        var orig = _undo.Count > 0 ? _undo.Peek().Pixels : _pixels;

        for (var y = py - half; y < py - half + size; y++)
        for (var x = px - half; x < px - half + size; x++)
        {
            if (x < 0 || y < 0 || x >= CanvasSize || y >= CanvasSize)
                continue;

            float coverage = 1f;
            if (pen)
            {
                var ox = Math.Abs(x - px + (even ? 0.5f : 0f)) / hr;
                var oy = Math.Abs(y - py + (even ? 0.5f : 0f)) / hr;
                if (ox * ox + oy * oy > 1f)
                    continue;
            }
            else if (soft)
            {
                var ox = Math.Abs(x - px + (even ? 0.5f : 0f)) / hr;
                var oy = Math.Abs(y - py + (even ? 0.5f : 0f)) / hr;
                var d = MathF.Sqrt(ox * ox + oy * oy);
                if (d > 1f)
                    continue;

                coverage = Math.Clamp((1f - d) / 0.45f, 0f, 1f);
            }

            var stampAlpha = alpha * coverage;

            var idx = y * CanvasSize + x;
            if (stampAlpha <= _strokeAlpha[idx])
                continue;
            _strokeAlpha[idx] = stampAlpha;

            var i = idx * 4;
            var r = (byte) (c.RByte * stampAlpha + orig[i] * (1f - stampAlpha));
            var g = (byte) (c.GByte * stampAlpha + orig[i + 1] * (1f - stampAlpha));
            var b = (byte) (c.BByte * stampAlpha + orig[i + 2] * (1f - stampAlpha));
            _pixels[i] = r;
            _pixels[i + 1] = g;
            _pixels[i + 2] = b;
            _pixels[i + 3] = byte.MaxValue;
            _buffer[x, y] = new Rgba32(r, g, b, byte.MaxValue);
        }
    }

    /// <summary>
    /// Пипетка: цвет пикселя в тулбокс, холст не меняется.
    /// </summary>
    private void PickColor(Vector2 relative)
    {
        var rect = CanvasRect();
        var px = Math.Clamp((int) ((relative.X - rect.Left) / rect.Size.X * (CanvasSize - 1)), 0, CanvasSize - 1);
        var py = Math.Clamp((int) ((relative.Y - rect.Top) / rect.Size.Y * (CanvasSize - 1)), 0, CanvasSize - 1);

        var i = (py * CanvasSize + px) * 4;
        var alpha = (byte) (_tools.Opacity * 255f);
        _tools.Color = new Color(_pixels[i], _pixels[i + 1], _pixels[i + 2], alpha);
        _tools.Touch();
    }

    /// <summary>
    /// Допуск заливки: сумма |dr|+|dg|+|db|.
    /// </summary>
    private const int FillTolerance = 96;

    /// <summary>
    /// Заливка связной области от точки (итеративно).
    /// </summary>
    private void FloodFill(int sx, int sy)
    {
        var start = (sy * CanvasSize + sx) * 4;
        var tr = _pixels[start];
        var tg = _pixels[start + 1];
        var tb = _pixels[start + 2];

        var c = _tools.Color;
        var alpha = c.AByte / 255f;

        // Свой цвет игнорим
        if (Math.Abs(c.RByte - tr) + Math.Abs(c.GByte - tg) + Math.Abs(c.BByte - tb) <= FillTolerance
            && alpha >= 1f)
            return;

        var visited = new bool[CanvasSize * CanvasSize];
        var stack = new Stack<Vector2i>();
        stack.Push(new Vector2i(sx, sy));

        while (stack.Count > 0)
        {
            var p = stack.Pop();
            if (p.X < 0 || p.Y < 0 || p.X >= CanvasSize || p.Y >= CanvasSize)
                continue;
            var idx = p.Y * CanvasSize + p.X;
            if (visited[idx])
                continue;
            visited[idx] = true;

            var i = idx * 4;
            if (Math.Abs(_pixels[i] - tr) + Math.Abs(_pixels[i + 1] - tg) + Math.Abs(_pixels[i + 2] - tb) > FillTolerance)
                continue;

            var r = (byte) (c.RByte * alpha + _pixels[i] * (1f - alpha));
            var g = (byte) (c.GByte * alpha + _pixels[i + 1] * (1f - alpha));
            var b = (byte) (c.BByte * alpha + _pixels[i + 2] * (1f - alpha));
            _pixels[i] = r;
            _pixels[i + 1] = g;
            _pixels[i + 2] = b;
            _pixels[i + 3] = byte.MaxValue;
            _buffer[p.X, p.Y] = new Rgba32(r, g, b, byte.MaxValue);

            stack.Push(new Vector2i(p.X + 1, p.Y));
            stack.Push(new Vector2i(p.X - 1, p.Y));
            stack.Push(new Vector2i(p.X, p.Y + 1));
            stack.Push(new Vector2i(p.X, p.Y - 1));
        }
    }

    private void PushUndo()
    {
        _undo.Push((_buffer.Clone(), (byte[]) _pixels.Clone()));
        if (_undo.Count <= 20)
            return;

        var list = _undo.ToList();
        list[^1].Image.Dispose();
        list.RemoveAt(list.Count - 1);
        _undo.Clear();
        for (var i = list.Count - 1; i >= 0; i--)
        {
            _undo.Push(list[i]);
        }
    }

    public void Undo()
    {
        if (_undo.Count == 0)
            return;

        _buffer.Dispose();
        var prev = _undo.Pop();
        _buffer = prev.Image;
        _pixels = prev.Pixels;
        _texture.SetSubImage(Vector2i.Zero, _buffer);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _texture.Dispose();
        _buffer.Dispose();
        foreach (var snap in _undo)
        {
            snap.Image.Dispose();
        }

        _undo.Clear();
    }
}
