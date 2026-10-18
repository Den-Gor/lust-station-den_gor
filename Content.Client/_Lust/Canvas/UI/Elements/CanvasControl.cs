using System.Numerics;
using Content.Client._Lust.Canvas;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

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
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {

    }

    protected override void MouseExited()
    {

    }

    protected override void ExitedTree()
    {
        base.ExitedTree();
        _texture.Dispose();
        _buffer.Dispose();

        foreach (var snap in _undo)
        {
            snap.Image.Dispose();
        }

        _undo.Clear();
    }
}
