using System.Globalization;
using Content.Shared._Lust.Implants.Components;
using Robust.Shared.Utility;

namespace Content.Shared._Lust.Implants.Systems;

public sealed partial class OrganicSpeedModifierSystem
{
    /// <summary>
    /// Публичный api создания строк message об  модификаторов скорости на BodyComponent в ExamineVerb соообщения
    /// </summary>
    /// <param name="body"></param>
    /// <param name="message"></param>
    public void AppendBodyExamine(EntityUid body, FormattedMessage message)
    {
        if (!_bodyQuery.TryGetComponent(body, out var bodyComp))
            return;

        var multiplier = GetCombined((body, bodyComp));

        if (MathF.Abs(multiplier - 1f) < 0.0001f)
            return;

        AppendSectionHeader(message, "lust-organic-modifier-examine-speed");
        AppendSpeedLine(message, multiplier);
    }

    /// <summary>
    /// Публичный api создания строк message об  модификаторов скорости на OrganicSpeedModifierComponent в ExamineVerb соообщения
    /// </summary>
    /// <param name="organ"></param>
    /// <param name="message"></param>
    public void AppendOrganExamine(EntityUid organ, FormattedMessage message)
    {
        if (!_speedQuery.TryGetComponent(organ, out var mod))
            return;

        if (MathF.Abs(mod.MovementMultiplier - 1f) < 0.0001f)
            return;

        AppendSectionHeader(message, "lust-organic-modifier-examine-speed");
        AppendSpeedLine(message, mod.MovementMultiplier);
    }

    private void AppendSpeedLine(FormattedMessage message, float multiplier)
    {
        var percent = (multiplier - 1f) * 100f;
        var valueStr = MathF.Abs(percent).ToString("0.##", CultureInfo.InvariantCulture);

        message.PushNewline();
        message.AddMarkupOrThrow(percent >= 0
            ? Loc.GetString("lust-organic-modifier-speed-increased", ("value", valueStr))
            : Loc.GetString("lust-organic-modifier-speed-decreased", ("value", valueStr)));
    }

    private void AppendSectionHeader(FormattedMessage message, string locKey)
    {
        if (message.Count > 0)
            message.PushNewline();

        message.AddMarkupOrThrow(Loc.GetString(locKey));
    }
}
