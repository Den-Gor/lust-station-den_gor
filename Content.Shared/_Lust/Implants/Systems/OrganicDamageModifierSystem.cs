using Content.Shared._Lust.Implants.Components;
using Content.Shared._Lust.Implants.Events;
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
    private EntityQuery<OrganicDamageModifierComponent> _modifierQuery;
    private EntityQuery<BodyComponent> _bodyQuery;

    public override void Initialize()
    {
        base.Initialize();

        _modifierQuery = GetEntityQuery<OrganicDamageModifierComponent>();
        _bodyQuery = GetEntityQuery<BodyComponent>();

        SubscribeLocalEvent<BodyComponent, DamageModifyEvent>(OnBodyDamageModify);

        SubscribeLocalEvent<OrganicDamageModifierComponent, OrganGotInsertedEvent>(OnOrganInserted);
        SubscribeLocalEvent<OrganicDamageModifierComponent, OrganGotRemovedEvent>(OnOrganRemoved);

        SubscribeLocalEvent<OrganicDamageModifierComponent, ComponentStartup>(OnOrganModifierChanged);
        SubscribeLocalEvent<OrganicDamageModifierComponent, ComponentShutdown>(OnOrganModifierChanged);

        // Инициализируе эвенты ExamineVerb
        InitializeExamine();
    }

    /// <summary>
    /// Публичный API Возвращает актуальный набор модификаторов урона на BodyComponent
    /// </summary>
    public DamageModifierSet GetCombined(Entity<BodyComponent> body)
    {
        if (body.Comp.Organs is not { } organs)
            return new DamageModifierSet();

        var cache = EnsureComp<OrganicDamageModifierCacheComponent>(body);
        if (cache.Dirty)
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
        if (ent.Comp.Organs is not { } organs)
            return;

        if (!HasAnyPositiveDamage(args.Damage))
            return;

        var cache = EnsureComp<OrganicDamageModifierCacheComponent>(ent);

        if (cache.Dirty)
            Rebuild(organs, cache);

        if (cache.Combined.Coefficients.Count == 0 && cache.Combined.FlatReduction.Count == 0)
            return;

        args.Damage = DamageSpecifier.ApplyModifierSet(
            args.Damage,
            cache.Combined,
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
            cache.Dirty = true;
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
                target.Coefficients[type] = target.Coefficients.TryGetValue(type, out var existing)
                    ? existing * coeff
                    : coeff;
            }

            foreach (var (type, flat) in mod.Modifiers.FlatReduction)
            {
                target.FlatReduction[type] = target.FlatReduction.GetValueOrDefault(type) + flat;
            }
        }

        cache.Dirty = false;
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
