using Content.Shared._Lust.Implants.Systems;
using Content.Shared.Damage;

namespace Content.Shared._Lust.Implants.Components;


[RegisterComponent, Access(typeof(OrganicImplantEmpSystem))]
public sealed partial class OrganicEmpCacheComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public DamageSpecifier Combined = new();

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsDirty = true;
}
