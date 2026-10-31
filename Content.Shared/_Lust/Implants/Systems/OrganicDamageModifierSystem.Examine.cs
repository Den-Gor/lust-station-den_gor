using System.Globalization;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Lust.Implants.Systems;

public sealed partial class OrganicDamageModifierSystem
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    /// <summary>
    /// Публичный api создания строк message об  модификаторов урона на BodyComponent в ExamineVerb соообщения
    /// </summary>
    /// <param name="body"></param>
    /// <param name="message"></param>
    public void AppendBodyExamine(EntityUid body, FormattedMessage message)
    {
        if (!_bodyQuery.TryGetComponent(body, out var bodyComp))
            return;

        var combined = GetCombined((body, bodyComp));

        if (combined.Coefficients.Count == 0 && combined.FlatReductions.Count == 0)
            return;

        AppendSectionHeader(message, "lust-organic-modifier-examine-damage");
        AppendModifierLines(message, combined);
    }

    /// <summary>
    /// Публичный api создания строк message об  модификаторов урона на OrganicDamageModifierComponent в ExamineVerb соообщения
    /// </summary>
    /// <param name="organ"></param>
    /// <param name="message"></param>
    public void AppendOrganExamine(EntityUid organ, FormattedMessage message)
    {
        if (!_modifierQuery.TryGetComponent(organ, out var mod))
            return;

        if (mod.Modifiers.Coefficients.Count == 0 && mod.Modifiers.FlatReductions.Count == 0)
            return;

        AppendSectionHeader(message, "lust-organic-modifier-examine-damage");
        AppendModifierLines(message, mod.Modifiers);
    }

    private void AppendModifierLines(FormattedMessage message, DamageModifierSet modifiers)
    {
        foreach (var (type, coeff) in modifiers.Coefficients)
        {
            if (MathF.Abs(coeff - 1f) < 0.0001f)
                continue;
            var damageType = GetDamageTypeName(type);
            var percent = (1f - coeff) * 100f;
            var valueStr = MathF.Abs(percent).ToString("0.##", CultureInfo.InvariantCulture);

            message.PushNewline();
            message.AddMarkupOrThrow(percent >= 0
                ? Loc.GetString("armor-coefficient-value", ("type", damageType), ("value", valueStr))
                : Loc.GetString("lust-organic-modifier-damage-increased", ("type", damageType), ("value", valueStr)));
        }

        foreach (var (type, reduction) in modifiers.FlatReductions)
        {
            if (MathF.Abs(reduction) < 0.0001f)
                continue;

            var damageType = GetDamageTypeName(type);
            var valueStr = MathF.Abs(reduction).ToString("0.##", CultureInfo.InvariantCulture);

            message.PushNewline();
            message.AddMarkupOrThrow(reduction >= 0
                ? Loc.GetString("armor-reduction-value",
                    ("type", damageType),
                    ("value", valueStr))
                : Loc.GetString("lust-organic-modifier-flat-increase",
                    ("type", damageType),
                    ("value", valueStr)));
        }
    }

    private string GetDamageTypeName(string type)
    {
        var armorNameKey = $"armor-damage-type-{type.ToLowerInvariant()}";
        return Loc.TryGetString(armorNameKey, out var armorName)
            ? armorName
            : _prototypeManager.Index<DamageTypePrototype>(type).LocalizedName;
    }

    private void AppendSectionHeader(FormattedMessage message, string locKey)
    {
        if (message.Count > 0)
            message.PushNewline();

        message.AddMarkupOrThrow(Loc.GetString(locKey));
    }
}
