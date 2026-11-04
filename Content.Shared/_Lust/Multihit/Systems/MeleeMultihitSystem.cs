using System.Linq;
using Content.Shared._Lust.Multihit.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Lust.Multihit.Systems;

/// <summary>
/// Система позволяющая бить сразуже несколькими оружиями  (удар двумя ножами)
/// </summary>
public sealed partial class MeleeMultihitSystem : EntitySystem
{
    [Dependency] private SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MeleeMultihitComponent, MeleeHitEvent>(OnMeleeHit);
    }

    /// <summary>
    /// Обробочик ивента удара
    /// </summary>
    /// <param name="weapon">оружие</param>
    /// <param name="args"></param>
    private void OnMeleeHit(Entity<MeleeMultihitComponent> weapon, ref MeleeHitEvent args)
    {
        // Скип первого придикшена(иначе будет спам)
        if (!_timing.IsFirstTimePredicted)
            return;

        if (args.Handled)
            return;

        // Защита от рекурсии
        if (TryComp<ActiveMeleeMultihitComponent>(weapon.Owner, out var active))
        {
            ApplyDamageMultiplier(ref args, active.Multiplier);
            RemComp<ActiveMeleeMultihitComponent>(weapon.Owner);
            return;
        }

        // Промах
        if (args.HitEntities.Count == 0)
            return;

        var isHeavy = args.Direction != null;


        if (!TryComp<HandsComponent>(args.User, out var hands))
            return;

        // Поиск второго оружия
        var secondWeapons = new List<EntityUid>();
        foreach (var handId in _hands.EnumerateHands((args.User, hands)))
        {
            if (!_hands.TryGetHeldItem(args.User, handId, out var held))
                continue;

            if (held == weapon.Owner)
                continue;

            if (!TryComp<MeleeMultihitComponent>(held.Value, out var bComp))
                continue;

            if (weapon.Comp.WeaponWhitelist != null
                && !_whitelist.CheckBoth(held.Value, whitelist: weapon.Comp.WeaponWhitelist))
                continue;

            if (bComp.WeaponWhitelist != null
                && !_whitelist.CheckBoth(weapon.Owner, whitelist: bComp.WeaponWhitelist))
                continue;

            secondWeapons.Add(held.Value);
        }

        if (secondWeapons.Count == 0)
            return;

        // Данные для удара
        var target = args.HitEntities[0];
        var user = args.User;
        var delay = weapon.Comp.Delay;
        var multiplier = weapon.Comp.DamageMultiplier;
        var maxExtra = Math.Min(secondWeapons.Count, weapon.Comp.MaxExtraHits);

        // Подготовка для heavy (широкий замах)
        EntityCoordinates? heavyCoords = null;
        List<NetEntity>? heavyEntities = null;

        if (isHeavy)
        {
            // Координаты из первой цели
            if (TryComp(target, out TransformComponent? targetXform))
                heavyCoords = targetXform.Coordinates;

            // Список целей все из HitEntities
            heavyEntities = args.HitEntities
                .Where(e => !Deleted(e))
                .Select(e => GetNetEntity(e))
                .ToList();
        }

        // Планируем удары
        for (var i = 0; i < maxExtra; i++)
        {
            var extraWeapon = secondWeapons[i];
            var extraDelay = delay * (i + 1);

            Timer.Spawn(extraDelay,
                () =>
            {
                if (Deleted(user) || Deleted(extraWeapon) || Deleted(target))
                    return;
                if (!TryComp<MeleeWeaponComponent>(extraWeapon, out var extraMelee))
                    return;

                // Изменяем кулдаун
                extraMelee.NextAttack = _timing.CurTime;
                Dirty(extraWeapon, extraMelee);

                // Вешаем Active с множителем из A
                var activeComp = EnsureComp<ActiveMeleeMultihitComponent>(extraWeapon);
                activeComp.Multiplier = multiplier;
                Dirty(extraWeapon, activeComp);

                bool success;
                if (isHeavy && heavyCoords.HasValue && heavyEntities is { Count: > 0 })
                {
                    success = _melee.AttemptHeavyAttack(
                        user,
                        extraWeapon,
                        extraMelee,
                        heavyEntities,
                        GetNetCoordinates(heavyCoords.Value));
                }
                else
                {
                    success = _melee.AttemptLightAttack(user, extraWeapon, extraMelee, target);
                }

                // Очистка компонента защищающего от рекурсии если событие не прошло
                if (!success && HasComp<ActiveMeleeMultihitComponent>(extraWeapon))
                    RemComp<ActiveMeleeMultihitComponent>(extraWeapon);
            });
        }
    }

    /// <summary>
    /// Метод изменяющий урон в соответствии с коифицентом multiplier
    /// </summary>
    /// <param name="args"></param>
    /// <param name="multiplier"></param>
    private void ApplyDamageMultiplier(ref MeleeHitEvent args, float multiplier)
    {
        if (MathF.Abs(multiplier - 1.0f) < 0.0001f)
            return;

        if (args.BaseDamage.DamageDict.Count == 0)
            return;

        var coefficients = new Dictionary<ProtoId<DamageTypePrototype>, float>();
        foreach (var type in args.BaseDamage.DamageDict.Keys)
        {
            coefficients[type] = multiplier;
        }

        args.ModifiersList.Add(new DamageModifierSet { Coefficients = coefficients });
    }
}

