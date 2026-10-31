using Content.Shared._Lust.Implants.Components;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Emp;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.Implants.Systems;

/// <summary>
/// Обрабатывает ЭМИ-импульсы по телам с кибер-имплантами.
/// Суммарный EmpReaction кешируется на теле и пересобирается только при изменении органов.
/// </summary>
public sealed class OrganicImplantEmpSystem : EntitySystem
{
    [Dependency] private EntityQuery<OrganicEmpComponent> _implantQuery = default!;
    [Dependency] private EntityQuery<BodyComponent> _bodyQuery = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    private readonly EntProtoId _sparkEffect = "EffectSparks";

    public override void Initialize()
    {
        base.Initialize();

        // Горячий путь — событие приходит на КАЖДОЕ тело в радиусе ЭМИ
        SubscribeLocalEvent<BodyComponent, EmpPulseEvent>(OnBodyEmpPulse);

        // Инвалидация — по тем же событиям, что у тебя в других системах
        SubscribeLocalEvent<OrganicEmpComponent, OrganGotInsertedEvent>(OnOrganInserted);
        SubscribeLocalEvent<OrganicEmpComponent, OrganGotRemovedEvent>(OnOrganRemoved);

        SubscribeLocalEvent<OrganicEmpComponent, ComponentStartup>(OnImplantComponentChanged);
        SubscribeLocalEvent<OrganicEmpComponent, ComponentShutdown>(OnImplantComponentChanged);
    }

    /// <summary>
    /// API возвращает актуальный суммарный EmpReaction на теле.
    /// </summary>
    public DamageSpecifier GetCombined(Entity<BodyComponent> body)
    {
        if (body.Comp.Organs is not { } organs)
            return new DamageSpecifier();

        var cache = EnsureComp<OrganicEmpCacheComponent>(body);
        if (cache.IsDirty)
            Rebuild(organs, cache);

        return cache.Combined;
    }

    private void OnBodyEmpPulse(Entity<BodyComponent> ent, ref EmpPulseEvent args)
    {
        var combined = GetCombined(ent);

        _damageable.TryChangeDamage(ent.Owner, combined, origin: ent.Owner);

        args.Affected = true;

        if (!HasAnyPositiveDamage(combined))
            return;

        SpawnAtPosition(_sparkEffect, Transform(ent.Owner).Coordinates);
    }

    private void OnOrganInserted(Entity<OrganicEmpComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (_bodyQuery.TryGetComponent(args.Target, out var body))
            Invalidate((args.Target, body));
    }

    private void OnOrganRemoved(Entity<OrganicEmpComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (_bodyQuery.TryGetComponent(args.Target, out var body))
            Invalidate((args.Target, body));
    }

    private void OnImplantComponentChanged(Entity<OrganicEmpComponent> ent, ref ComponentStartup args)
        => InvalidateParentBody(ent.Owner);

    private void OnImplantComponentChanged(Entity<OrganicEmpComponent> ent, ref ComponentShutdown args)
        => InvalidateParentBody(ent.Owner);

    private void InvalidateParentBody(EntityUid organ)
    {
        var parent = Transform(organ).ParentUid;
        if (_bodyQuery.TryGetComponent(parent, out var body))
            Invalidate((parent, body));
    }

    private void Invalidate(Entity<BodyComponent> ent)
    {
        if (TryComp<OrganicEmpCacheComponent>(ent, out var cache))
            cache.IsDirty = true;
    }

    private void Rebuild(Container organs, OrganicEmpCacheComponent cache)
    {
        cache.Combined = new DamageSpecifier();

        foreach (var organ in organs.ContainedEntities)
        {
            if (!_implantQuery.TryGetComponent(organ, out var implant))
                continue;

            foreach (var (type, value) in implant.EmpReaction.DamageDict)
            {
                cache.Combined.DamageDict[type] =
                    cache.Combined.DamageDict.GetValueOrDefault(type) + value;
            }
        }

        cache.IsDirty = false;
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
