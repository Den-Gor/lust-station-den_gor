using Content.Client._Lust.Canvas.UI;
using Content.Shared._Lust.Canvas.Components;
using Robust.Client.UserInterface;

namespace Content.Client._Lust.Canvas;

/// <summary>
/// Окно просмотра сохранённого рисунка
/// </summary>
public sealed class CanvasBoundViewUserInterface : BoundUserInterface
{
    private CanvasViewWindow? _window;

    public CanvasBoundViewUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CanvasViewWindow>();

        if (EntMan.TryGetComponent<CanvasComponent>(Owner, out var canvas) && canvas.ImageId != null)
        {
            _window.SetImage(canvas.ImagePath!);
        }
        else
        {
            _window.Close();
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _window?.Close();
        _window = null;
    }
}
