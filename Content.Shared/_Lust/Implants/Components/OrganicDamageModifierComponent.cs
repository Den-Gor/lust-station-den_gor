using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._Lust.Implants.Components;

/// <summary>
/// Данной компонент хранит в себе модификатор урона, при добавление его органом, органы будут
/// внедрённые в игрока будут довать ему дополнительную защиту
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class OrganicDamageModifierComponent : Component
{
    [DataField]
    public DamageModifierSet Modifiers = new();
}
