using Content.Shared._Lust.Implants.Systems;
using Content.Shared.Damage;

namespace Content.Shared._Lust.Implants.Components;

/// <summary>
/// Кеш агрегированных модификаторов органов. Живёт на теле.
/// Пересчитывается лениво при первом DamageModifyEvent после инвалидации.
/// </summary>
[RegisterComponent,Access(typeof(OrganicDamageModifierSystem))]
public sealed partial class OrganicDamageModifierCacheComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public DamageModifierSet Combined = new();

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsDirty = true;
}
