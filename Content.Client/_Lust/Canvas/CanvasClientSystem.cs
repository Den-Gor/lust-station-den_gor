using Content.Client._Lust.Canvas.UI;
using Content.Client._Sunrise;
using Content.Shared._Lust.Canvas;
using Content.Shared._Lust.Canvas.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Client._Lust.Canvas;

public sealed partial class CanvasClientSystem : EntitySystem
{
    [Dependency] private INetManager _netManager = default!;
    [Dependency] private NetTexturesManager _netTexturesManager = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    private ISawmill _sawmill = default!;

    [ValidatePrototypeId<CanvasConfigPrototype>]
    private const string ConfigId = "LustDefault";

    public CanvasToolState Tools { get; private set; } = default!;

    public int OpenCanvases;

    private CanvasToolboxWindow? _toolbox;
    private readonly Dictionary<EntityUid, CanvasWindow> _editors = new();

    public override void Initialize()
    {
        base.Initialize();
        Tools = new CanvasToolState(_prototypes.Index<CanvasConfigPrototype>(ConfigId));
        _sawmill = LogManager.GetSawmill("canvas.client");
        _netTexturesManager.ResourceLoaded += OnResourceLoaded;
    }

    public override void Shutdown()
    {
        _netTexturesManager.ResourceLoaded -= OnResourceLoaded;
        base.Shutdown();
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

    }

    public void RegisterEditor(EntityUid paper, CanvasWindow window)
    {
        window.SetLoading(true);
        _editors[paper] = window;

        if (!TryComp<CanvasComponent>(paper, out var canvas) || canvas.ImagePath is not { } imagePath)
        {
            _sawmill.Warning($"Cannot load canvas for editing: no image path on {ToPrettyString(paper)}");
            window.SetImageLoadFailed();
            return;
        }

        if (_netTexturesManager.EnsureResource(imagePath))
            LoadCachedImage(paper, window, imagePath);
    }

    public void UnregisterEditor(EntityUid paper, CanvasWindow window)
    {
        if (_editors.TryGetValue(paper, out var current) && ReferenceEquals(current, window))
            _editors.Remove(paper);
    }

    private void OnResourceLoaded(string resourcePath)
    {
        foreach (var (paper, window) in _editors)
        {
            if (!TryComp<CanvasComponent>(paper, out var canvas)
                || !string.Equals(canvas.ImagePath, resourcePath, StringComparison.Ordinal))
                continue;

            LoadCachedImage(paper, window, resourcePath);
        }
    }

    private void LoadCachedImage(EntityUid paper, CanvasWindow window, string resourcePath)
    {
        if (!_netTexturesManager.TryGetCachedCanvasImage(resourcePath, out var data))
        {
            _sawmill.Warning($"Could not read cached canvas image for editing on {ToPrettyString(paper)}");
            window.SetImageLoadFailed();
            return;
        }

        window.LoadImage(data);
    }
}
