using Content.Shared._Lust.Chemistry.ReagentEffects;
using Content.Shared._Lust.Implants.Components;
using Content.Shared.EntityEffects;

namespace Content.Shared._Lust.Chemistry.Systems;


public sealed partial class LustNeurostabilinEffectSystem : EntityEffectSystem<OrganicImmunityComponent, LustNeurostabilinEffect>
{
    protected override void Effect(Entity<OrganicImmunityComponent> entity, ref EntityEffectEvent<LustNeurostabilinEffect> args)
    {
        var heal = args.Effect.ImmunityPerUnit * args.Scale;
        entity.Comp.Current = MathF.Min(entity.Comp.Max, entity.Comp.Current + heal);
    }
}
