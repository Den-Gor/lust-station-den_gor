using Content.Shared._Lust.Canvas.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.UserInterface;
using Robust.Shared.Network;
using static Content.Shared.Paper.PaperComponent;

namespace Content.Shared._Lust.Canvas.Systems;

public sealed partial class CanvasSystem : EntitySystem
{
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private PaperSystem _paper = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private INetManager _netManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaperComponent, CanvasConvertAttemptEvent>(OnCanvasConvertAttempt);
        SubscribeLocalEvent<PaperComponent, ActivatableUIOpenAttemptEvent>(OnPaperOpenAttempt);
        SubscribeLocalEvent<PaperComponent, UseInHandEvent>(OnPaperUseInHand,
            after: [typeof(ActivatableUISystem)],
            before: [typeof(IngestionSystem)]);
        SubscribeLocalEvent<CanvasComponent, BoundUIClosedEvent>(OnCanvasClosed);
    }

    /// <summary>
    /// Защита от поедания рисунка
    /// </summary>
    private void OnPaperUseInHand(Entity<PaperComponent> ent, ref UseInHandEvent args)
    {
        if (!TryComp<CanvasComponent>(ent, out var canvas) || canvas.ImageId == null)
            return;

        args.Handled = true;
    }

    /// <summary>
    /// Функция удаления пустого холста
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="args"></param>
    private void OnCanvasClosed(Entity<CanvasComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!_netManager.IsServer)
            return;

        if (!args.UiKey.Equals(CanvasUiKey.Key))
            return;

        if (ent.Comp.HasDrawing || !string.IsNullOrEmpty(ent.Comp.ImageId))
            return;

        RemComp<CanvasComponent>(ent);

    }

    /// <summary>
    /// пробуем конвертировать бумагу в холст
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="args"></param>
    private void OnCanvasConvertAttempt(Entity<PaperComponent> ent, ref CanvasConvertAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (_tag.HasTag(args.Brush, "LustDraw"))
        {
            // Нельзя редактировать рисунок после печатей
            if (ent.Comp.StampedBy.Count > 0)
            {
                _popup.PopupClient(Loc.GetString("lust-canvas-popup-stamped-edit"), ent.Owner, args.User);
                args.Cancelled = true;
                return;
            }

            if (TryConvertToCanvas(ent, args.Brush, args.User))
            {
                args.Cancelled = true;
            }
            return;
        }

        // не даём открыть второе окно просмотра
        if (_userInterface.IsUiOpen(ent.Owner, CanvasUiKey.Key, args.User))
        {
            _popup.PopupClient(Loc.GetString("lust-canvas-popup-canvas-open"), ent.Owner, args.User);
            args.Cancelled = true;
        }
    }

private void OnPaperOpenAttempt(Entity<PaperComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!HasComp<CanvasComponent>(ent))
            return;

        if (args.Silent)
            return;

        // не даём переоткрыть бумагу обычной ручкой
        if (_userInterface.IsUiOpen(ent.Owner, CanvasUiKey.Key, args.User))
        {
            _popup.PopupClient(Loc.GetString("lust-canvas-popup-canvas-open"), ent.Owner, args.User);
            args.Cancel();
            return;
        }

        // Нарисованная бумага открывается как картинка, а не текст
        if (TryComp<CanvasComponent>(ent, out var canvas) && canvas.ImageId != null)
        {
            // Если просмотр уже открыт закрываем его
            if (_userInterface.IsUiOpen(ent.Owner, CanvasUiKey.View, args.User))
            {
                _userInterface.CloseUi(ent.Owner, CanvasUiKey.View, args.User);
                args.Cancel();
                return;
            }

            _userInterface.OpenUi(ent.Owner, CanvasUiKey.View, args.User);
            args.Cancel();
        }
    }

    /// <summary>
    /// Пытаемся кновертировать бумагу в холст
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="brush"></param>
    /// <param name="user"></param>
    /// <returns></returns>
    public bool TryConvertToCanvas(Entity<PaperComponent> ent, EntityUid brush, EntityUid user)
    {
        if (!CanConvertToCanvas(ent, brush, user))
            return false;

        if (!HasComp<CanvasComponent>(ent))
        {
            AddComp<CanvasComponent>(ent);
            _paper.SetContent(ent, "");
        }

        _userInterface.OpenUi(ent.Owner, CanvasUiKey.Key, user);
        return true;
    }

    /// <summary>
    /// Проверка можно ли конвертировать бумагув холст
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="brush"></param>
    /// <param name="user"></param>
    /// <param name="quiet"></param>
    public bool CanConvertToCanvas(Entity<PaperComponent> ent, EntityUid brush, EntityUid user, bool quiet = false)
    {
        if (!_tag.HasTag(brush, "LustDraw"))
            return false;

        if (ent.Comp.StampedBy.Count > 0)
            return false;

        // Проверка не открыта ли эта бумажка уже в режими текста
        if (_userInterface.IsUiOpen(ent.Owner, PaperUiKey.Key, user))
        {
            if (!quiet)
                _popup.PopupClient(Loc.GetString("lust-canvas-popup-paper-open"), ent.Owner, user);
            return false;
        }

        return true;
    }
}

