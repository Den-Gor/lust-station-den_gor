using Content.Client._Lust.Canvas.UI;
using Content.Shared._Lust.Canvas.Components;
using Robust.Client.UserInterface;

namespace Content.Client._Lust.Canvas;

public sealed class CanvasBoundUserInterface : BoundUserInterface
{
    private CanvasWindow? _window;

    public CanvasBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        var canvasSystem = EntMan.System<CanvasClientSystem>();
        _window = this.CreateWindow<CanvasWindow>();
        _window.SetPaper(Owner);
        _window.SetTools(canvasSystem.Tools);
        _window.SetSystems(canvasSystem);
        _window.SetBoundUserInterface(this);

        if (EntMan.TryGetComponent<CanvasComponent>(Owner, out var canvas) && canvas.ImageId != null)
            canvasSystem.RegisterEditor(Owner, _window);

        canvasSystem.NotifyCanvasOpened();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        var canvasSystem = EntMan.System<CanvasClientSystem>();
        if (_window != null)
            canvasSystem.UnregisterEditor(Owner, _window);
        canvasSystem.NotifyCanvasClosed();
        _window?.Close();
        _window = null;
    }
}
