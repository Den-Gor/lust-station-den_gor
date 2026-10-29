using Robust.Shared.GameStates;

namespace Content.Shared._Lust.Implants.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class OrganicSpeedModifierComponent : Component
{
    [DataField,ViewVariables(VVAccess.ReadOnly)]
    public float MovementMultiplier = 1.0f;
}
