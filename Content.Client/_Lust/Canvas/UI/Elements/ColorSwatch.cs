using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Input;

namespace Content.Client._Lust.Canvas.UI.Elements;

public sealed partial class ColorSwatch : Control
{
    private Color _color = Color.White;
    private bool _selected;

    public event Action? Selected;

    public ColorSwatch()
    {
        MinSize = new Vector2(28, 28);
        MouseFilter = MouseFilterMode.Stop;
    }

    public void Select(bool selected)
    {
        _selected = selected;
    }

    public void SetColor(Color color)
    {
        _color = color;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        handle.DrawRect(UIBox2.FromDimensions(Vector2.Zero,Size), _color);

        if (_selected)
        {
            var w = Size.X;
            var h = Size.Y;

            handle.DrawRect(new UIBox2(0f, 0f, w, 2f), Color.Black);
            handle.DrawRect(new UIBox2(0f, h - 2f, w, h), Color.Black);
            handle.DrawRect(new UIBox2(0f, 2f, 2f, h - 2f), Color.Black);
            handle.DrawRect(new UIBox2(w - 2f, 2f, w, h - 2f), Color.Black);

            handle.DrawRect(new UIBox2(2f, 2f, w - 2f, 4f), Color.White);
            handle.DrawRect(new UIBox2(2f, h - 4f, w - 2f, h - 2f), Color.White);
            handle.DrawRect(new UIBox2(2f, 4f, 4f, h - 4f), Color.White);
            handle.DrawRect(new UIBox2(w - 4f, 4f, w - 2f, h - 4f), Color.White);
        }
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        Selected?.Invoke();
        args.Handle();
    }

}
