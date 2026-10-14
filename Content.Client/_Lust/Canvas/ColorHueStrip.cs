using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client._Lust.Canvas;

/// <summary>
/// Радужная полоска Hue. Градиент статичный, строится один раз.
/// </summary>
public sealed partial class ColorHueStrip : Control
{
    [Dependency] private IClyde _clyde = default!;

    private const int Resolution = 180;

    private OwnedTexture? _tex;
    private bool _drag;

    public float H { get; private set; }

    public event Action? Changed;

    public ColorHueStrip()
    {
        IoCManager.InjectDependencies(this);
        MinSize = new Vector2(200, 24);
        MouseFilter = MouseFilterMode.Stop;

        using var img = new Image<Rgba32>(Resolution, 1);
        for (var x = 0; x < Resolution; x++)
        {
            var c = Color.FromHsv(new Vector4(x / (float) (Resolution - 1), 1f, 1f, 1f));
            img[x, 0] = new Rgba32(c.RByte, c.GByte, c.BByte, 255);
        }

        _tex = _clyde.CreateBlankTexture<Rgba32>((Resolution, 1));
        _tex.SetSubImage(Vector2i.Zero, img);
    }

    public void SetHue(float h)
    {
        H = Math.Clamp(h, 0f, 1f);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        if (_tex == null)
            return;

        handle.DrawTextureRect(_tex, UIBox2.FromDimensions(Vector2.Zero, Size));

        var x = H * Size.X;
        handle.DrawRect(new UIBox2(x - 1f, 0f, x + 1f, Size.Y), Color.White);
        handle.DrawRect(new UIBox2(x - 2f, 0f, x + 2f, Size.Y), Color.Black, filled: false);
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
        H = Math.Clamp(relative.X / Size.X, 0f, 1f);
        Changed?.Invoke();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _tex?.Dispose();
    }
}
