using System.Globalization;
using Content.Shared._Lust.Implants.Components;
using Content.Shared._Lust.Implants.Events;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Lust.Implants.Systems;

public sealed partial class OrganicDamageModifierSystem
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    private void InitializeExamine()
    {
        SubscribeLocalEvent<BodyComponent, BodyModifiersExamineEvent>(OnBodyExamineDamage);
        SubscribeLocalEvent<OrganicDamageModifierComponent, OrganModifiersExamineEvent>(OnOrganExamineDamage);
    }

    private void OnBodyExamineDamage(Entity<BodyComponent> ent, ref BodyModifiersExamineEvent args)
    {
        var combined = GetCombined(ent);
        if (combined.Coefficients.Count == 0 && combined.FlatReduction.Count == 0)
            return;

        args.Message.AddMarkupOrThrow(Loc.GetString("lust-organic-modifier-examine-damage"));
        AppendModifierLines(args.Message, combined);
    }

    private void OnOrganExamineDamage(Entity<OrganicDamageModifierComponent> ent, ref OrganModifiersExamineEvent args)
    {
        if (ent.Comp.Modifiers.Coefficients.Count == 0 && ent.Comp.Modifiers.FlatReduction.Count == 0)
            return;

        args.Message.AddMarkupOrThrow(Loc.GetString("lust-organic-modifier-examine-heading"));
        AppendModifierLines(args.Message, ent.Comp.Modifiers);
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
