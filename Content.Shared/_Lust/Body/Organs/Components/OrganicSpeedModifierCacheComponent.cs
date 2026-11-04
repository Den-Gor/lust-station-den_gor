namespace Content.Shared._Lust.Body.Organs.Components;

[RegisterComponent,Access(typeof(Systems.OrganicSpeedModifierSystem))]
public sealed partial class OrganicSpeedModifierCacheComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public float MovementMultiplier = 1.0f;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsDirty = true;
}
