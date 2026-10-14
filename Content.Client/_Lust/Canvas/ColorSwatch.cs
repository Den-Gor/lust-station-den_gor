using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Maths;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client._Lust.Canvas;

/// <summary>
/// Ячейка палитры: ЛКМ — выбрать (цвет ячейки становится текущим).
/// </summary>
public sealed partial class ColorSwatch : Control
{
    private Color _bg = Color.White;
    private bool _selected;

    public event Action? Picked;

    public ColorSwatch()
    {
        MinSize = new Vector2(28, 28);
        MouseFilter = MouseFilterMode.Stop;
    }

    public void SetColor(Color color)
    {
        _bg = color;
    }

    public void SetSelected(bool selected)
    {
        _selected = selected;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        handle.DrawRect(UIBox2.FromDimensions(Vector2.Zero, Size), _bg);
        if (_selected)
        {
            var w = Size.X;
            var h = Size.Y;
            // Черная снаружи
            handle.DrawRect(new UIBox2(0f, 0f, w, 2f), Color.Black);
            handle.DrawRect(new UIBox2(0f, h - 2f, w, h), Color.Black);
            handle.DrawRect(new UIBox2(0f, 2f, 2f, h - 2f), Color.Black);
            handle.DrawRect(new UIBox2(w - 2f, 2f, w, h - 2f), Color.Black);
            // Белая внутри
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

        Picked?.Invoke();
        args.Handle();
    }
}
