namespace Content.Client._Lust.Canvas;

public sealed class CanvasClientSystem : EntitySystem
{
    public readonly CanvasToolState Tools = new();
    public int OpenCanvases; // количетсвоо открытых холстов

    private CanvasToolboxWindow? _toolbox;

    public void NotifyCanvasOpened()
    {
        OpenCanvases++;
        if (_toolbox is { Disposed: false })
            return;

        _toolbox = new CanvasToolboxWindow();
        _toolbox.SetTools(Tools);
        _toolbox.OpenCenteredRight();
    }

    public void NotifyCanvasClosed()
    {
        OpenCanvases = Math.Max(0, OpenCanvases - 1);
        if (OpenCanvases > 0)
            return;

        _toolbox?.Close();
        _toolbox = null;
    }
}
