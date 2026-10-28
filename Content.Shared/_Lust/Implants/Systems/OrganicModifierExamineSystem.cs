using System.Globalization;
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
    [Dependency] private OrganicDamageModifierSystem _organicDamage = default!;


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

        var combined = _organicDamage.GetCombined(entity);

        if (combined.Coefficients.Count == 0 && combined.FlatReduction.Count == 0)
            return;

        var message = new FormattedMessage();
        message.AddMarkupOrThrow(Loc.GetString("lust-organic-modifier-examine-heading"));
        AppendModifierLines(message, combined);
        AddModifierExamineVerb(args, entity.Comp, message);
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
        foreach (var (type, coeff) in modifiers.Coefficients)
        {
            if (MathF.Abs(coeff - 1f) < 0.0001f) continue;
            var damageType = GetDamageTypeName(type);
            var percent = (1f - coeff) * 100f;
            var valueStr = MathF.Abs(percent).ToString("0.##", CultureInfo.InvariantCulture);


            message.PushNewline();
            message.AddMarkupOrThrow(percent >= 0
                ? Loc.GetString("armor-coefficient-value", ("type", damageType), ("value", valueStr))
                : Loc.GetString("lust-organic-modifier-damage-increased", ("type", damageType), ("value", valueStr)));
        }

        foreach (var (type, reduction) in modifiers.FlatReduction)
        {
            if (MathF.Abs(reduction) < 0.0001f) continue;

            var damageType = GetDamageTypeName(type);
            var valueStr = MathF.Abs(reduction).ToString("0.##", CultureInfo.InvariantCulture);


            message.PushNewline();
            message.AddMarkupOrThrow(reduction >= 0
                ? Loc.GetString("armor-reduction-value",
                    ("type", damageType), ("value", valueStr))
                : Loc.GetString("lust-organic-modifier-flat-increase",
                    ("type", damageType), ("value", valueStr)));
        }
    }


    private string GetDamageTypeName(string type)
    {
        var armorNameKey = $"armor-damage-type-{type.ToLowerInvariant()}";
        return Loc.TryGetString(armorNameKey, out var armorName)
            ? armorName
            : _prototypeManager.Index<DamageTypePrototype>(type).LocalizedName;
    }
}
