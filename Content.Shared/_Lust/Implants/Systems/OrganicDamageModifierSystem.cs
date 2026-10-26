using System.Linq;
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

        SubscribeLocalEvent<BodyComponent, DamageModifyEvent>(OnDamageModify);
    }

    /// <summary>
    /// Метод обрабочики ивента переберающий все органы имеющие OrganicDamageModifierComponent и собирает с них все модификаторы урона сумируя их
    /// и применя их к урону идущему на BodyComponent
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="args"></param>
    private void OnDamageModify(Entity<BodyComponent> entity, ref DamageModifyEvent args)
    {
        if (entity.Comp.Organs is not { } organs)
            return;

        var combined = new DamageModifierSet();

        foreach (var organ in organs.ContainedEntities)
        {
            if (!TryComp<OrganicDamageModifierComponent>(organ, out var modifier))
                continue;

            AddModifiers(combined, modifier.Modifiers);
        }

        if (combined.Coefficients.Count == 0 && combined.FlatReduction.Count == 0)
            return;

        foreach (var type in combined.Coefficients.Keys.ToArray())
        {
            combined.Coefficients[type] = MathF.Max(0f, combined.Coefficients[type]);
        }

        args.Damage = DamageSpecifier.ApplyModifierSet(
            args.Damage,
            combined,
            args.ArmorPenetration,
            args.CanHeal);
    }

    private static void AddModifiers(DamageModifierSet target, DamageModifierSet source)
    {
        foreach (var (type, coefficient) in source.Coefficients)
        {
            target.Coefficients[type] =
                target.Coefficients.GetValueOrDefault(type, 1f) + coefficient - 1f;
        }

        foreach (var (type, reduction) in source.FlatReduction)
        {
            target.FlatReduction[type] =
                target.FlatReduction.GetValueOrDefault(type) + reduction;
        }
    }

}

