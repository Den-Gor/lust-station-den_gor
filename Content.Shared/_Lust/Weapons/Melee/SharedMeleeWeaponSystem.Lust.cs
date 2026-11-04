using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Map;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Shared.Weapons.Melee;

/// <summary>
/// Расширения методов у MeleeWeaponSystem
/// </summary>
public abstract partial class SharedMeleeWeaponSystem
{
    /// <summary>
    /// Публичный Api для вызова удароа с замахом
    /// </summary>
    /// <param name="user">Тот кто  бьёт</param>
    /// <param name="weaponUid">оружие</param>
    /// <param name="weapon">компонент оружия</param>
    /// <param name="entities">те по кому попали</param>
    /// <param name="coordinates"> направление удара</param>
    /// <returns></returns>
    public bool AttemptHeavyAttack(EntityUid user, EntityUid weaponUid, MeleeWeaponComponent weapon, List<NetEntity> entities, NetCoordinates coordinates)
    {
        return AttemptAttack(user, weaponUid, weapon, new HeavyAttackEvent(GetNetEntity(weaponUid), entities, coordinates), null);
    }
}
