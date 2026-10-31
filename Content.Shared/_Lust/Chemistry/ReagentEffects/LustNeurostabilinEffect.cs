using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.Chemistry.ReagentEffects;

/// <summary>
/// Восстанавливает иммунитет от киберпсихоза.
/// </summary>
public sealed partial class LustNeurostabilinEffect : EntityEffectBase<LustNeurostabilinEffect>
{
    [DataField]
    public float ImmunityPerUnit = 5f;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-lust-neurostabilin", ("chance", Probability));
}
