using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client._Lust.Canvas;

/// <summary>
/// Квадрат S/V как в фотошопе: X — насыщенность, Y — яркость (вверх светлее).
/// Перестраивается только при смене Hue.
/// </summary>
public sealed class ColorSvSquare : Control
{
    [Dependency] private IClyde _clyde = default!;

    private const int Resolution = 128;

    private Image<Rgba32>? _img;
    private OwnedTexture? _tex;
    private bool _drag;

    public float H { get; private set; }
    public float S { get; private set; } = 1f;
    public float V { get; private set; } = 1f;

    public event Action? Changed;

    public ColorSvSquare()
    {
        IoCManager.InjectDependencies(this);
        MinSize = new Vector2(200, 200);
        MouseFilter = MouseFilterMode.Stop;
    }

    public void SetHsv(float h, float s, float v)
    {
        var hueChanged = !MathHelper.CloseTo(H, h);
        H = h;
        S = Math.Clamp(s, 0f, 1f);
        V = Math.Clamp(v, 0f, 1f);
        if (hueChanged)
            Rebuild();
    }

    public void SetHue(float h)
    {
        if (MathHelper.CloseTo(H, h))
            return;
        H = h;
        Rebuild();
    }

    public Color GetColor(float alpha = 1f)
    {
        return Color.FromHsv(new Vector4(H, S, V, alpha));
    }

    private void Rebuild()
    {
        _img ??= new Image<Rgba32>(Resolution, Resolution);
        _tex ??= _clyde.CreateBlankTexture<Rgba32>((Resolution, Resolution));

        for (var y = 0; y < Resolution; y++)
        for (var x = 0; x < Resolution; x++)
        {
            var c = Color.FromHsv(new Vector4(H, x / (float) (Resolution - 1), 1f - y / (float) (Resolution - 1), 1f));
            _img[x, y] = new Rgba32(c.RByte, c.GByte, c.BByte, 255);
        }

        _tex.SetSubImage(Vector2i.Zero, _img);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        if (_tex == null)
            Rebuild();

        handle.DrawTextureRect(_tex!, UIBox2.FromDimensions(Vector2.Zero, Size));

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
        if (!_drag)
            return;
        PickAt(args.RelativePosition);
        args.Handle();
    }

    private void PickAt(Vector2 relative)
    {
        S = Math.Clamp(relative.X / Size.X, 0f, 1f);
        V = Math.Clamp(1f - relative.Y / Size.Y, 0f, 1f);
        Changed?.Invoke();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _tex?.Dispose();
        _img?.Dispose();
    }
}
