using Content.Shared._Lust.Implants.Components;
using Content.Shared.Body;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage;

namespace Content.Shared._Lust.Implants.Systems;

/// <summary>
/// Данная система высчитывает модификаторы урона органов и применяет их на урон входящий по игроку
/// </summary>
public sealed class OrganicDamageModifierSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, DamageModifyEvent>(OnBodyDamageModify);
        SubscribeLocalEvent<OrganicDamageModifierComponent, BodyRelayedEvent<DamageModifyEvent>>(OnOrganDamageModify);

    }

    /// <summary>
    /// Метод обрабочики ивента переберающий все органы имеющие OrganicDamageModifierComponent и отправляющий им ивент
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="args"></param>
    private void OnBodyDamageModify(Entity<BodyComponent> ent, ref DamageModifyEvent args)
    {
        if (ent.Comp.Organs is not { } organs)
            return;

        if (!HasAnyPositiveDamage(args.Damage))
            return;

        foreach (var organ in organs.ContainedEntities)
        {
            var relayed = new BodyRelayedEvent<DamageModifyEvent>(ent, args);
            RaiseLocalEvent(organ, ref relayed);
            args = relayed.Args;
        }

    }
    /// <summary>
    /// Метод вызываюемы под ивент DamageModifyEvent на органе имеющим OrganicDamageModifierComponent внутри персонажа
    /// и применяет  ApplyModifierSet значений из  OrganicDamageModifierComponent на урон по персонажу
    /// </summary>
    private void OnOrganDamageModify(Entity<OrganicDamageModifierComponent> ent, ref BodyRelayedEvent<DamageModifyEvent> args)
    {
        args.Args.Damage = DamageSpecifier.ApplyModifierSet(
            args.Args.Damage,
            ent.Comp.Modifiers,
            args.Args.ArmorPenetration,
            args.Args.CanHeal);
    }

    private static bool HasAnyPositiveDamage(DamageSpecifier damage)
    {
        foreach (var value in damage.DamageDict.Values)
        {
            if (value > 0f)
                return true;
        }
        return false;
    }
}

