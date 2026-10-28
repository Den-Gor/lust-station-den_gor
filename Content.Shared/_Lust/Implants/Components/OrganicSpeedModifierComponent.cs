using Robust.Shared.GameStates;

namespace Content.Shared._Lust.Implants.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class OrganicSpeedModifierComponent : Component
{
    [DataField]
    public float MovementMultiplier = 1.0f;
}
