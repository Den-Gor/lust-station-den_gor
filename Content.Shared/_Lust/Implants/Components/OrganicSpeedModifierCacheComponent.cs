using Content.Shared._Lust.Implants.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._Lust.Implants.Components;

[RegisterComponent,Access(typeof(OrganicSpeedModifierSystem))]
public sealed partial class OrganicSpeedModifierCacheComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public float MovementMultiplier = 1.0f;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsDirty = true;
}
