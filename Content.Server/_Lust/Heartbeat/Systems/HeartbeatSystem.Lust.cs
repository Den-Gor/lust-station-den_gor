using Content.Server._Sunrise.Heartbeat.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;

#pragma warning disable IDE0130
namespace Content.Server._Sunrise.Heartbeat.Systems;

public sealed partial class HeartbeatSystem
{
    [Dependency] private MobThresholdSystem _mobThreshold = default!;

    /// <summary>
    /// Проверяем персонажа действительно ли он должен сейчас быть в крите или нет
    /// возвращает true если не находит компоненты / он в крите, false если он не должен быть в крите по хп.
    /// Используется GetTotalDamage устаревший метод в связи с отсутсвием аналогов на текущий момент, при апстримах
    /// и мердж конфликтов смотрите в Content.Server._Sunrise.Heartbeat.Systems и ищите новый метод который  они начнут использовать вместо GetTotalDamage
    /// </summary>
    private bool IsTrueCrit(Entity<CritHeartbeatComponent> ent)
    {

        if (!TryComp<DamageableComponent>(ent.Owner, out var damageable))
            return true;

        var totalDamage = _damageable.GetTotalDamage((ent.Owner, damageable));

        return !_mobThreshold.TryGetThresholdForState(ent.Owner, MobState.Critical, out var critThreshold)
               || totalDamage >= critThreshold;
    }
}
