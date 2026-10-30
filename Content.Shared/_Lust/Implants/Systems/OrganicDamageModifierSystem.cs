using Content.Shared._Lust.Implants.Components;
using Content.Shared.Body;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage;
using Robust.Shared.Containers;

namespace Content.Shared._Lust.Implants.Systems;

/// <summary>
/// Данная система высчитывает модификаторы урона органов и применяет их на урон входящий по игроку
/// </summary>
public sealed partial class OrganicDamageModifierSystem : EntitySystem
{
    [Dependency] private EntityQuery<OrganicDamageModifierComponent> _modifierQuery = default!;
    [Dependency] private EntityQuery<BodyComponent> _bodyQuery = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, DamageModifyEvent>(OnBodyDamageModify);

        SubscribeLocalEvent<OrganicDamageModifierComponent, OrganGotInsertedEvent>(OnOrganInserted);
        SubscribeLocalEvent<OrganicDamageModifierComponent, OrganGotRemovedEvent>(OnOrganRemoved);

        SubscribeLocalEvent<OrganicDamageModifierComponent, ComponentStartup>(OnOrganModifierChanged);
        SubscribeLocalEvent<OrganicDamageModifierComponent, ComponentShutdown>(OnOrganModifierChanged);
    }

    /// <summary>
    /// Api Возвращает актуальный набор модификаторов урона на BodyComponent
    /// </summary>
    public DamageModifierSet GetCombined(Entity<BodyComponent> body)
    {
        if (body.Comp.Organs is not { } organs)
            return new DamageModifierSet();

        var cache = EnsureComp<OrganicDamageModifierCacheComponent>(body);
        if (cache.IsDirty)
            Rebuild(organs, cache);

        return cache.Combined;
    }

    /// <summary>
    /// Событие обрабочик урона, игнорирует полностью позетивный урон
    /// Ищит/создает  OrganicDamageModifierCacheComponent в BodyComponent и при cache.Dirty = true пересобирает cache модификаторов урона
    /// если итоговые значение коэфицентов не ровны 0 применяет их к событию урона.
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="args"></param>
    private void OnBodyDamageModify(Entity<BodyComponent> ent, ref DamageModifyEvent args)
    {
        if (!HasAnyPositiveDamage(args.Damage))
            return;

        var combined = GetCombined(ent);

        if (combined.Coefficients.Count == 0 && combined.FlatReduction.Count == 0)
            return;

        args.Damage = DamageSpecifier.ApplyModifierSet(
            args.Damage,
            combined,
            args.ArmorPenetration,
            args.CanHeal);
    }

    /// <summary>
    /// Обробочики ивентов об добавление / удаление органов // добавление удаление компонента у органа
    /// </summary>
    private void OnOrganInserted(Entity<OrganicDamageModifierComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (_bodyQuery.TryGetComponent(args.Target, out var body))
            Invalidate((args.Target, body));
    }

    private void OnOrganRemoved(Entity<OrganicDamageModifierComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (_bodyQuery.TryGetComponent(args.Target, out var body))
            Invalidate((args.Target, body));
    }

    private void OnOrganModifierChanged(Entity<OrganicDamageModifierComponent> ent, ref ComponentStartup args)
        => InvalidateParentBody(ent);

    private void OnOrganModifierChanged(Entity<OrganicDamageModifierComponent> ent, ref ComponentShutdown args)
        => InvalidateParentBody(ent);

    private void InvalidateParentBody(EntityUid organ)
    {
        var parent = Transform(organ).ParentUid;
        if (_bodyQuery.TryGetComponent(parent, out var body))
            Invalidate((parent, body));
    }

    private void Invalidate(Entity<BodyComponent> ent)
    {
        if (TryComp<OrganicDamageModifierCacheComponent>(ent, out var cache))
            cache.IsDirty = true;
    }

    /// <summary>
    /// Пересобирает кэш модификаторов урона со всех органов
    /// </summary>
    /// <param name="organs"></param>
    /// <param name="cache"></param>
    private void Rebuild(Container organs, OrganicDamageModifierCacheComponent cache)
    {
        var target = cache.Combined;
        target.Coefficients.Clear();
        target.FlatReduction.Clear();

        foreach (var organ in organs.ContainedEntities)
        {
            if (!_modifierQuery.TryGetComponent(organ, out var mod))
                continue;

            foreach (var (type, coeff) in mod.Modifiers.Coefficients)
            {
                var existing = target.Coefficients.GetValueOrDefault(type, 1f);

                target.Coefficients[type] = MathF.Max(0f, existing + coeff - 1f);
            }

            foreach (var (type, flat) in mod.Modifiers.FlatReduction)
            {
                target.FlatReduction[type] = target.FlatReduction.GetValueOrDefault(type) + flat;
            }
        }

        cache.IsDirty = false;
    }

    /// <summary>
    /// Функция проверяющая является ли DamageSpecifier чисто положительным
    /// </summary>
    /// <param name="damage"></param>
    /// <returns></returns>
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
