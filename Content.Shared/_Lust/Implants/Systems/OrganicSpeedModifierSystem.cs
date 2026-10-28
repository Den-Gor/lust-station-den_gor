using Content.Shared._Lust.Implants.Components;
using Content.Shared.Body;
using Content.Shared.Movement.Systems;
using Robust.Shared.Containers;

namespace Content.Shared._Lust.Implants.Systems;

public sealed partial class OrganicSpeedModifierSystem : EntitySystem
{
    [Dependency] private MovementSpeedModifierSystem _movementSpeed = default!;

    private EntityQuery<OrganicSpeedModifierComponent> _speedQuery;
    private EntityQuery<BodyComponent> _bodyQuery;

    public override void Initialize()
    {
        base.Initialize();
        _speedQuery = GetEntityQuery<OrganicSpeedModifierComponent>();
        _bodyQuery = GetEntityQuery<BodyComponent>();

        // Точка входа: собираем множители со всех органов тела
        SubscribeLocalEvent<BodyComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);

        // Инвалидация: состав органов изменился
        SubscribeLocalEvent<OrganicSpeedModifierComponent, OrganGotInsertedEvent>(OnOrganChanged);
        SubscribeLocalEvent<OrganicSpeedModifierComponent, OrganGotRemovedEvent>(OnOrganChanged);

        // Инвалидация: компонент добавили/убрали у органа в теле
        SubscribeLocalEvent<OrganicSpeedModifierComponent, ComponentStartup>(OnOrganComponentStartup);
        SubscribeLocalEvent<OrganicSpeedModifierComponent, ComponentShutdown>(OnOrganComponentShutdown);
    }

    // Сбор множителей с органов
    private void OnRefreshSpeed(Entity<BodyComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.Organs is not { } organs)
            return;

        var cache = EnsureComp<OrganicSpeedModifierCacheComponent>(ent);

        if (cache.Dirty)
        {
            Rebuild(organs, cache);
            cache.Dirty = false;
        }

        if (MathF.Abs(cache.MovementMultiplier - 1f) < 0.0001f)
            return;

        args.ModifySpeed(cache.MovementMultiplier, cache.MovementMultiplier);
    }

    // Ручной вызов обновления скорости при изменении органов
    private void OnOrganChanged(Entity<OrganicSpeedModifierComponent> ent, ref OrganGotInsertedEvent args)
        => InvalidateAndRefresh(args.Target);

    private void OnOrganChanged(Entity<OrganicSpeedModifierComponent> ent, ref OrganGotRemovedEvent args)
        => InvalidateAndRefresh(args.Target);

    private void OnOrganComponentStartup(Entity<OrganicSpeedModifierComponent> ent, ref ComponentStartup args)
    {
        if (TryGetParentBody(ent, out var body))
            InvalidateAndRefresh(body);
    }

    private void OnOrganComponentShutdown(Entity<OrganicSpeedModifierComponent> ent, ref ComponentShutdown args)
    {
        if (TryGetParentBody(ent, out var body))
            InvalidateAndRefresh(body);
    }

    private bool TryGetParentBody(EntityUid organ, out EntityUid body)
    {
        body = Transform(organ).ParentUid;
        return _bodyQuery.HasComponent(body);
    }

    private void InvalidateAndRefresh(EntityUid body)
    {
        if (TryComp<OrganicSpeedModifierCacheComponent>(body, out var cache))
            cache.Dirty = true;

        _movementSpeed.RefreshMovementSpeedModifiers(body);
    }

    private void Rebuild(Container organs, OrganicSpeedModifierCacheComponent cache)
    {
        var bonus = 0f;

        foreach (var organ in organs.ContainedEntities)
        {
            if (!_speedQuery.TryGetComponent(organ, out var mod))
                continue;

            bonus += mod.MovementMultiplier - 1f;
        }

        cache.MovementMultiplier = MathF.Max(0.1f, 1f + bonus);
    }
}
