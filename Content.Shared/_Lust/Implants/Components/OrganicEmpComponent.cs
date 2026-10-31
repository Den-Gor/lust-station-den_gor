using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._Lust.Implants.Components;

/// <summary>
/// Отдельный компонент для EMP-реакции импланта.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class OrganicEmpComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public DamageSpecifier EmpReaction = new();
}
