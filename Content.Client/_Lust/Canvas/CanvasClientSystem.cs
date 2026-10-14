namespace Content.Client._Lust.Canvas;

public sealed class CanvasClientSystem : EntitySystem
{
    public readonly CanvasToolState Tools = new();
    public int OpenCanvases;

    private CanvasToolboxWindow? _toolbox;

    public void NotifyCanvasOpened()
    {
        OpenCanvases++;
        ShowToolbox();
    }

    public void NotifyCanvasClosed()
    {
        OpenCanvases = Math.Max(0, OpenCanvases - 1);
        if (OpenCanvases > 0)
            return;

        _toolbox?.Close();
        _toolbox = null;
    }

    /// <summary>
    /// Кнопка «Инструменты» на холсте: показать/спрятать тулбокс вручную.
    /// </summary>
    public void ToggleToolbox()
    {
        if (_toolbox is { Disposed: false } box && box.IsOpen)
        {
            box.Close();
            _toolbox = null;
            return;
        }

        ShowToolbox();
    }

    private void ShowToolbox()
    {
        if (_toolbox is not { Disposed: false } || !_toolbox.IsOpen)
        {
            _toolbox = new CanvasToolboxWindow();
            _toolbox.SetTools(Tools);
            var box = _toolbox;
            box.OnClose += () =>
            {
                if (ReferenceEquals(_toolbox, box))
                    _toolbox = null;
            };
        }

        if (!_toolbox.IsOpen)
            _toolbox.OpenCenteredRight();
    }
}
