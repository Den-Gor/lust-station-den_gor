using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client._Lust.ColorPicker;

/// <summary>
/// Квадрат S/V для выбора насыщености цвета
/// </summary>
public sealed partial class ColorSvSquare : Control
{
    [Dependency] private IClyde _clyde = default!;

    private const int Resolution = 128;

    private readonly Image<Rgba32> _image;
    private readonly OwnedTexture _texture;
    private bool _drag;

    public float H { get; private set; }
    public float S { get; private set; } = 1f;
    public float V { get; private set; } = 1f;

    /// <summary>
    /// Событие вызываемое при изменение выбранного цвета и возвращающая его hsv
    /// </summary>
    public event Action<float, float, float>? HsvChanged;

    public ColorSvSquare()
    {
        IoCManager.InjectDependencies(this);
        MouseFilter = MouseFilterMode.Stop;

        _image = new Image<Rgba32>(Resolution, Resolution);
        _texture = _clyde.CreateBlankTexture<Rgba32>((Resolution, Resolution));
        Rebuild();
    }

    /// <summary>
    /// Задать состояние снаружи. Событие <see cref="HsvChanged"/> не вызывается!
    /// </summary>
    public void SetHsv(float h, float s, float v)
    {
        var hueChanged = !MathHelper.CloseTo(H, h);
        H = h;
        S = Math.Clamp(s, 0f, 1f);
        V = Math.Clamp(v, 0f, 1f);
        if (hueChanged)
            Rebuild();
    }

    /// <summary>
    /// Изменине hue
    /// </summary>
    public void SetHue(float h)
    {
        if (MathHelper.CloseTo(H, h))
            return;
        H = h;
        Rebuild();
    }

    /// <summary>
    /// Пересобрать градиент квадрата под текущий оттенок
    /// </summary>
    private void Rebuild()
    {
        for (var y = 0; y < Resolution; y++)
        for (var x = 0; x < Resolution; x++)
        {
            var c = Color.FromHsv(new Vector4(H, x / (float) (Resolution - 1), 1f - y / (float) (Resolution - 1), 1f));
            _image[x, y] = new Rgba32(c.RByte, c.GByte, c.BByte, 255);
        }

        _texture.SetSubImage(Vector2i.Zero, _image);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        handle.DrawTextureRect(_texture, UIBox2.FromDimensions(Vector2.Zero, Size));

        var marker = new Vector2(S * Size.X, (1f - V) * Size.Y);
        handle.DrawCircle(marker, 6f, Color.White, filled: false);
        handle.DrawCircle(marker, 5f, Color.Black, filled: false);
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        _drag = true;
        PickAt(args.RelativePosition);
        args.Handle();
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        _drag = false;
        args.Handle();
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        if (!_drag)
            return;

        PickAt(args.RelativePosition);
        args.Handle();
    }

    /// <summary>
    /// Конвертация кординат мышы в S и V
    /// </summary>
    private void PickAt(Vector2 relative)
    {
        S = Math.Clamp(relative.X / Size.X, 0f, 1f);
        V = Math.Clamp(1f - relative.Y / Size.Y, 0f, 1f);
        HsvChanged?.Invoke(H, S, V);
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();
        _texture.Dispose();
        _image.Dispose();
    }
}
