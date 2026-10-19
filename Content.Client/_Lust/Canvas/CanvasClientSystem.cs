using Content.Client._Lust.Canvas.UI;
using Content.Shared._Lust.Canvas;
using Robust.Shared.Network;

namespace Content.Client._Lust.Canvas;

public sealed partial class CanvasClientSystem : EntitySystem
{
    [Dependency] private INetManager _netManager = default!;

    private ISawmill _sawmill = default!;

    public readonly CanvasToolState Tools = new();

    public int OpenCanvases;

    private CanvasToolboxWindow? _toolbox;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = LogManager.GetSawmill("canvas.client");
    }

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
    /// Кнопка «Инструменты» на холсте показывает и прятчет тулбоксы
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

    public void SendSave(EntityUid paper, byte[] data)
    {
        if (!_netManager.IsConnected)
        {
            _sawmill.Warning("Cannot save canvas: client not connected to server");
            return;
        }

        _netManager.ClientSendMessage(new CanvasSaveMessage
        {
            PaperUid = GetNetEntity(paper),
            Data = data,
        });

        _sawmill.Debug($"Sent canvas save: {data.Length} bytes, paper: {ToPrettyString(paper)}");
    }
}
