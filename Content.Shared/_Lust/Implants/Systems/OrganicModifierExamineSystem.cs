using Content.Shared._Lust.Implants.Components;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Examine;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Lust.Implants.Systems;

/// <summary>
/// Система оброботки верба осмотра персонажа / органов имеющих OrganicDamageModifierComponent
/// </summary>
public sealed class OrganicModifierExamineSystem : EntitySystem
{
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
        SubscribeLocalEvent<OrganicDamageModifierComponent, GetVerbsEvent<ExamineVerb>>(OnGetItemExamineVerbs);
    }


    /// <summary>
    /// Функция обробочик события просмотра органов в BodyComponent собирающая все модификаторы урона OrganicDamageModifierComponent
    /// и пишит о них в ExamineVerb
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="args"></param>
    private void OnGetExamineVerbs(Entity<BodyComponent> entity, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (entity.Comp.Organs is not { } organs)
            return;

        var coefficients = new Dictionary<string, float>();
        var flatReductions = new Dictionary<string, float>();

        foreach (var organ in organs.ContainedEntities)
        {
            if (!TryComp<OrganicDamageModifierComponent>(organ, out var modifier))
                continue;

            foreach (var (type, coefficient) in modifier.Modifiers.Coefficients)
            {
                coefficients[type] =
                    coefficients.GetValueOrDefault(type, 1f) + coefficient - 1f;
            }

            foreach (var (type, reduction) in modifier.Modifiers.FlatReduction)
            {
                flatReductions[type] =
                    flatReductions.GetValueOrDefault(type) + reduction;
            }
        }

        if (coefficients.Count == 0 && flatReductions.Count == 0)
            return;

        AddModifierExamineVerb(
            args,
            entity.Comp,
            BuildBodyModifierMessage(coefficients, flatReductions));
    }

    /// <summary>
    /// Функция обробочик событие верба осмотра премдета с OrganicDamageModifierComponent собирает его модификаторы урона
    /// и отравляет ExamineVerb игроку
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="args"></param>
    private void OnGetItemExamineVerbs(Entity<OrganicDamageModifierComponent> entity, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (entity.Comp.Modifiers.Coefficients.Count == 0 && entity.Comp.Modifiers.FlatReduction.Count == 0)
            return;

        var message = new FormattedMessage();
        message.AddMarkupOrThrow(Loc.GetString("lust-organic-modifier-examine-heading"));
        AppendModifierLines(message, entity.Comp.Modifiers);
        AddModifierExamineVerb(args, entity.Comp, message);
    }

    /// <summary>
    /// Создает ExamineVerb сообщение
    /// </summary>
    /// <param name="args"></param>
    /// <param name="component"></param>
    /// <param name="message"></param>
    private void AddModifierExamineVerb(GetVerbsEvent<ExamineVerb> args, Component component, FormattedMessage message)
    {
        _examine.AddDetailedExamineVerb(
            args,
            component,
            message,
            Loc.GetString("lust-organic-modifier-examine-verb-text"),
            "/Textures/Interface/VerbIcons/dot.svg.192dpi.png",
            Loc.GetString("lust-organic-modifier-examine-verb-message"));
    }

    private void AppendModifierLines(FormattedMessage message, DamageModifierSet modifiers)
    {
        foreach (var (type, reduction) in modifiers.FlatReduction)
        {
            var damageType = GetDamageTypeName(type);

            message.PushNewline();
            message.AddMarkupOrThrow(Loc.GetString("armor-reduction-value", ("type", damageType), ("value", reduction)));
        }

        foreach (var (type, coefficient) in modifiers.Coefficients)
        {
            var damageType = GetDamageTypeName(type);
            var effectiveCoefficient = MathF.Max(0f, coefficient);
            var percent = MathF.Round((1f - effectiveCoefficient) * 100, 1);
            var line = percent >= 0
                ? Loc.GetString("armor-coefficient-value", ("type", damageType), ("value", percent))
                : Loc.GetString("lust-organic-modifier-damage-increased", ("type", damageType), ("value", MathF.Abs(percent)));

            message.PushNewline();
            message.AddMarkupOrThrow(line);
        }
    }

    /// <summary>
    /// Получает списки коэфицентов модификаторов урона и редуции урона и формирует в сообщение
    /// </summary>
    /// <param name="coefficients"></param>
    /// <param name="flatReductions"></param>
    /// <returns></returns>
    private FormattedMessage BuildBodyModifierMessage(
        Dictionary<string, float> coefficients,
        Dictionary<string, float> flatReductions)
    {
        var message = new FormattedMessage();
        message.AddMarkupOrThrow(Loc.GetString("lust-organic-modifier-examine-heading"));

        foreach (var (type, coefficient) in coefficients)
        {
            var damageType = GetDamageTypeName(type);
            var percent = MathF.Round((1f - coefficient) * 100, 1);
            var line = percent >= 0
                ? Loc.GetString("armor-coefficient-value", ("type", damageType), ("value", percent))
                : Loc.GetString("lust-organic-modifier-damage-increased", ("type", damageType), ("value", MathF.Abs(percent)));

            message.PushNewline();
            message.AddMarkupOrThrow(line);
        }

        foreach (var (type, reduction) in flatReductions)
        {
            if (reduction <= 0)
                continue;

            var damageType = GetDamageTypeName(type);

            message.PushNewline();
            message.AddMarkupOrThrow(Loc.GetString(
                "lust-organic-modifier-total-flat-reduction-value",
                ("type", damageType),
                ("value", reduction)));
        }

        return message;
    }

    private string GetDamageTypeName(string type)
    {
        var armorNameKey = $"armor-damage-type-{type.ToLowerInvariant()}";
        return Loc.TryGetString(armorNameKey, out var armorName)
            ? armorName
            : _prototypeManager.Index<DamageTypePrototype>(type).LocalizedName;
    }
}
