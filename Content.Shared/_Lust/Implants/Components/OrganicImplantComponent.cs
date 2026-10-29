using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._Lust.Implants.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial  class OrganicImplantComponent :  Component
{

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float PassiveDrain = 0.1f;

    [DataField,ViewVariables(VVAccess.ReadOnly)]
    public DamageSpecifier EmpReaction = new();

}
