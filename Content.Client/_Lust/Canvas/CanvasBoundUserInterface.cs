using Robust.Client.UserInterface;

namespace Content.Client._Lust.Canvas;

public sealed class CanvasBoundUserInterface : BoundUserInterface
{
    private CanvasWindow? _window;

    public  CanvasBoundUserInterface(EntityUid owner, Enum  uiKey) : base(owner, uiKey)
    {}

    protected override void Open()
    {
        base.Open();
        var canvasSystem = EntMan.System<CanvasClientSystem>();
        _window = this.CreateWindow<CanvasWindow>();
        _window.SetTools(canvasSystem.Tools);
        canvasSystem.NotifyCanvasOpened();
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        EntMan.System<CanvasClientSystem>().NotifyCanvasClosed();
        _window?.Close();
        _window = null;
    }
}
