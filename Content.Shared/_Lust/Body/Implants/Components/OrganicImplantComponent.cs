using Robust.Shared.GameStates;

namespace Content.Shared._Lust.Body.Implants.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial  class OrganicImplantComponent :  Component
{

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float PassiveDrain = 0.1f;
}
