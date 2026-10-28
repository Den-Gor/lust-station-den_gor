using Content.Shared._Lust.Implants.Events;
using Content.Shared.Body;
using Content.Shared.Examine;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared._Lust.Implants.Systems;

public sealed class OrganicModifiersExamineSystem : EntitySystem
{
    [Dependency] private ExamineSystemShared _examine = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, GetVerbsEvent<ExamineVerb>>(OnBodyExamine);
        SubscribeLocalEvent<OrganComponent, GetVerbsEvent<ExamineVerb>>(OnOrganExamine);
    }

    /// <summary>
    /// Осмотр тела
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="args"></param>
    private void OnBodyExamine(Entity<BodyComponent> entity, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var message = new FormattedMessage();
        var ev = new BodyModifiersExamineEvent(message);
        RaiseLocalEvent(entity, ref ev);

        if (ev.Message.Count == 0)
            return;

        AddVerb(args, entity.Comp, ev.Message);
    }

    /// <summary>
    /// Осмотр органов
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="args"></param>
    private void OnOrganExamine(Entity<OrganComponent> entity, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var message = new FormattedMessage();
        var ev = new OrganModifiersExamineEvent(message);
        RaiseLocalEvent(entity, ref ev);

        if (ev.Message.Count == 0)
            return;

        AddVerb(args, entity.Comp, ev.Message);
    }

    /// <summary>
    /// Создание верба
    /// </summary>
    /// <param name="args"></param>
    /// <param name="component"></param>
    /// <param name="message"></param>
    private void AddVerb(GetVerbsEvent<ExamineVerb> args, Component component, FormattedMessage message)
    {
        _examine.AddDetailedExamineVerb(
            args,
            component,
            message,
            Loc.GetString("lust-organic-modifier-examine-verb-text"),
            "/Textures/Interface/VerbIcons/dot.svg.192dpi.png",
            Loc.GetString("lust-organic-modifier-examine-verb-message"));
    }
}
