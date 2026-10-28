using Content.Shared._Lust.Implants.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._Lust.Implants.Components;

[RegisterComponent,Access(typeof(OrganicSpeedModifierSystem))]
public sealed partial class OrganicSpeedModifierCacheComponent : Component
{
    public float MovementMultiplier = 1.0f;
    public new bool Dirty = true;
}
