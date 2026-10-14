using Content.Shared._Lust.Canvas.Components;
using Content.Shared._Lust.Canvas;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.UserInterface;
using static Content.Shared.Paper.PaperComponent;

namespace Content.Shared._Lust.Canvas.Systems;

public sealed partial class CanvasSystem : EntitySystem
{
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private PaperSystem _paper = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PaperComponent, CanvasConvertAttemptEvent>(OnCanvasConvertAttempt);
        SubscribeLocalEvent<PaperComponent, ActivatableUIOpenAttemptEvent>(OnPaperOpenAttempt);
    }

    private void OnCanvasConvertAttempt(Entity<PaperComponent> ent, ref CanvasConvertAttemptEvent args)
    {
        if (args.Cancelled)
        {
            return;
        }

        // Кисть → конвертация / переоткрытие своего редактора
        if (_tag.HasTag(args.Brush, "LustDraw"))
        {
            if (TryConvertToCanvas(ent, args.Brush, args.User))
            {
                args.Cancelled = true;
            }
            return;
        }

        // Чужой инструмент (ручка, штамп), а у САМОГО игрока открыт холст → не даем второе окно
        if (_userInterface.IsUiOpen(ent.Owner, CanvasUiKey.Key, args.User))
        {
            _popup.PopupClient(Loc.GetString("lust-canvas-popup-canvas-open"), ent.Owner, args.User);
            args.Cancelled = true;
        }
    }

    private void OnPaperOpenAttempt(Entity<PaperComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
        {
            return;
        }

        // Обычная бумага без холста — не наше дело
        if (!HasComp<CanvasComponent>(ent))
        {
            return;
        }

        // У самого игрока открыт холст → второе окно (просмотр) не даем
        if (_userInterface.IsUiOpen(ent.Owner, CanvasUiKey.Key, args.User))
        {
            if (!args.Silent)
                _popup.PopupClient(Loc.GetString("lust-canvas-popup-canvas-open"), ent.Owner, args.User);
            args.Cancel();
        }
    }

    public bool TryConvertToCanvas(Entity<PaperComponent> ent, EntityUid brush, EntityUid user)
    {
        if (!CanConvertToCanvas(ent, brush, user))
        {
            return false;
        }

        if (!HasComp<CanvasComponent>(ent))
        {
            AddComp<CanvasComponent>(ent);
            _paper.SetContent(ent, "");
        }

        _userInterface.OpenUi(ent.Owner, CanvasUiKey.Key, user);
        return true;
    }

    public bool CanConvertToCanvas(Entity<PaperComponent> ent, EntityUid brush, EntityUid user, bool quiet = false)
    {
        if (!_tag.HasTag(brush, "LustDraw"))
        {
            return false;
        }

        if (ent.Comp.StampedBy.Count > 0)
        {
            return false;
        }

        // У самого игрока открыта бумага как текст → второе окно (холст) не даем
        if (_userInterface.IsUiOpen(ent.Owner, PaperUiKey.Key, user))
        {
            if (!quiet)
                _popup.PopupClient(Loc.GetString("lust-canvas-popup-paper-open"), ent.Owner, user);
            return false;
        }

        return true;
    }
}

