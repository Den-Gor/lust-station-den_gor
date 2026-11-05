using Content.Shared.Body;
using Content.Shared.Examine;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared._Lust.Body.Organs.Systems;

public sealed partial class OrganicModifiersExamineSystem : EntitySystem
{
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private OrganicDamageModifierSystem _damageModifier = default!;
    [Dependency] private OrganicSpeedModifierSystem _speedModifier = default!;

    /// <summary>
    /// Осмотр тела
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="args"></param>
    [SubscribeLocalEvent]
    private void OnBodyExamine(Entity<BodyComponent> entity, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var message = new FormattedMessage();

        _damageModifier.AppendBodyExamine(entity, message);
        _speedModifier.AppendBodyExamine(entity, message);

        if (message.Count == 0)
            return;

        AddVerb(args, entity.Comp, message);
    }

    /// <summary>
    /// Осмотр органов
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="args"></param>
    [SubscribeLocalEvent]
    private void OnOrganExamine(Entity<OrganComponent> entity, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var message = new FormattedMessage();

        _damageModifier.AppendOrganExamine(entity, message);
        _speedModifier.AppendOrganExamine(entity, message);

        if (message.Count == 0)
            return;

        AddVerb(args, entity.Comp, message);
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
