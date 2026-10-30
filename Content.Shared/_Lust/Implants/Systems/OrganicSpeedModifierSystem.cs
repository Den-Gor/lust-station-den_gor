using Content.Shared._Lust.Implants.Components;
using Content.Shared.Body;
using Content.Shared.Movement.Systems;
using Robust.Shared.Containers;

namespace Content.Shared._Lust.Implants.Systems;

public sealed partial class OrganicSpeedModifierSystem : EntitySystem
{
    [Dependency] private MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private EntityQuery<OrganicSpeedModifierComponent> _speedQuery = default!;
    [Dependency] private EntityQuery<BodyComponent> _bodyQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);

        SubscribeLocalEvent<OrganicSpeedModifierComponent, OrganGotInsertedEvent>(OnOrganChanged);
        SubscribeLocalEvent<OrganicSpeedModifierComponent, OrganGotRemovedEvent>(OnOrganChanged);

        SubscribeLocalEvent<OrganicSpeedModifierComponent, ComponentStartup>(OnOrganComponentStartup);
        SubscribeLocalEvent<OrganicSpeedModifierComponent, ComponentShutdown>(OnOrganComponentShutdown);
    }

    /// <summary>
    /// Возвращает актуальный множитель скорости для тела.
    /// Пересобирает кеш, если он Dirty.
    /// </summary>
    public float GetCombined(Entity<BodyComponent> body)
    {
        if (body.Comp.Organs is not { } organs)
            return 1f;

        var cache = EnsureComp<OrganicSpeedModifierCacheComponent>(body);
        if (cache.IsDirty)
            Rebuild(organs, cache);

        return cache.MovementMultiplier;
    }

    // Сбор множителей с органов
    private void OnRefreshSpeed(Entity<BodyComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        var multiplier = GetCombined(ent);

        if (MathF.Abs(multiplier - 1f) < 0.0001f)
            return;

        args.ModifySpeed(multiplier, multiplier);
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

    /// <summary>
    /// получение органа из тела
    /// </summary>
    /// <param name="organ"></param>
    /// <param name="body"></param>
    /// <returns></returns>
    private bool TryGetParentBody(EntityUid organ, out EntityUid body)
    {
        body = Transform(organ).ParentUid;
        return _bodyQuery.HasComponent(body);
    }

    /// <summary>
    /// Инвалидация кэша
    /// </summary>
    /// <param name="body"></param>
    private void InvalidateAndRefresh(EntityUid body)
    {
        if (TryComp<OrganicSpeedModifierCacheComponent>(body, out var cache))
            cache.IsDirty = true;

        _movementSpeed.RefreshMovementSpeedModifiers(body);
    }

    /// <summary>
    /// Пересобирает кэш модификаторов скорости со всех органов
    /// </summary>
    /// <param name="organs"></param>
    /// <param name="cache"></param>
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
        cache.IsDirty = false;
    }
}
