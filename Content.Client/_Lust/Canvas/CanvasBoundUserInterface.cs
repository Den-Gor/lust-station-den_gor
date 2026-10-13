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
        _window = this.CreateWindow<CanvasWindow>();
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _window?.Close();
        _window = null;
    }
}
