using Content.Shared._Lust.Implants;
using Content.Shared._Lust.Implants.Components;
using Content.Shared.Body;
using Content.Shared.Medical;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Lust.Implants;

/// <summary>
/// Система измениение имунитета от имплантов
/// </summary>
public sealed class OrganicImmunitySystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private VomitSystem _vomit = default!;

    private EntityQuery<OrganicImplantComponent> _implantQuery;


    public override void Initialize()
    {
        base.Initialize();
        _implantQuery = GetEntityQuery<OrganicImplantComponent>();

        // Инвалидация при изменении состава имплантов
        SubscribeLocalEvent<OrganicImplantComponent, OrganGotInsertedEvent>(OnImplantInserted);
        SubscribeLocalEvent<OrganicImplantComponent, OrganGotRemovedEvent>(OnImplantRemoved);

        SubscribeLocalEvent<OrganicImplantComponent, ComponentStartup>(OnImplantStartup);
        SubscribeLocalEvent<OrganicImplantComponent, ComponentShutdown>(OnImplantShutdown);

    }
    private void OnImplantInserted(Entity<OrganicImplantComponent> ent, ref OrganGotInsertedEvent args)
        => InvalidateDrain(args.Target);

    private void OnImplantRemoved(Entity<OrganicImplantComponent> ent, ref OrganGotRemovedEvent args)
        => InvalidateDrain(args.Target);

    private void OnImplantStartup(Entity<OrganicImplantComponent> ent, ref ComponentStartup args)
        => InvalidateParent(ent);

    private void OnImplantShutdown(Entity<OrganicImplantComponent> ent, ref ComponentShutdown args)
        => InvalidateParent(ent);

    private void InvalidateDrain(EntityUid body)
    {
        if (TryComp<OrganicImmunityComponent>(body, out var imm))
            imm.DrainDirty = true;
    }

    private void InvalidateParent(EntityUid organ)
    {
        var parent = Transform(organ).ParentUid;
        InvalidateDrain(parent);
    }

    /// <summary>
    /// Рантайм изменения имунитета
    /// </summary>
    /// <param name="frameTime"></param>
    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<OrganicImmunityComponent, BodyComponent>();

        while (query.MoveNext(out var uid, out var immunity, out var body))
        {
            if (now < immunity.NextTick)
                continue;

            immunity.NextTick = now + TimeSpan.FromSeconds(1);

            if (immunity.DrainDirty)
            {
                RebuildDrain(immunity, body);
                immunity.DrainDirty = false;
            }

            var net = immunity.BaseRegen - immunity.CachedDrain;
            var newCurrent = Math.Clamp(immunity.Current + net, 0f, immunity.Max);

            if (MathF.Abs(newCurrent - immunity.Current) < 0.0001f)
                continue;

            immunity.Current = newCurrent;
            var oldTier = immunity.CurrentTier;

            UpdateTier(immunity);
            Dirty(uid, immunity);

            if (immunity.CurrentTier != oldTier && immunity.CurrentTier > oldTier)
                ApplyRandomSymptom((uid, immunity));

            if (now >= immunity.NextSymptomCheck)
            {
                immunity.NextSymptomCheck = now + TimeSpan.FromSeconds(5);
                TrySymptoms((uid, immunity));
            }
        }
    }

    /// <summary>
    /// Перестройка Кеша урона по имунитету
    /// </summary>
    /// <param name="immunity"></param>
    /// <param name="body"></param>
    private void RebuildDrain(OrganicImmunityComponent immunity, BodyComponent body)
    {
        var total = 0f;
        if (body.Organs is { } organs)
        {
            foreach (var organ in organs.ContainedEntities)
            {
                if (_implantQuery.TryGetComponent(organ, out var implant))
                    total += implant.PassiveDrain;
            }
        }
        immunity.CachedDrain = total;
    }

    /// <summary>
    /// Обновления тира болезни
    /// </summary>
    /// <param name="immunity"></param>
    private void UpdateTier(OrganicImmunityComponent immunity)
    {
        immunity.CurrentTier = immunity.Current switch
        {
            >= 70f => OrganicImmunityTier.Healthy,
            >= 50f => OrganicImmunityTier.Mild,
            >= 20f => OrganicImmunityTier.Moderate,
            >= 10f => OrganicImmunityTier.Severe,
            _ => OrganicImmunityTier.Critical,
        };
    }

    /// <summary>
    /// Проверка появиться ли симтом
    /// </summary>
    /// <param name="ent"></param>
    private void TrySymptoms(Entity<OrganicImmunityComponent> ent)
    {
        var chance = ent.Comp.CurrentTier switch
        {
            OrganicImmunityTier.Healthy   => 0f,
            OrganicImmunityTier.Mild      => 0.05f,
            OrganicImmunityTier.Moderate  => 0.15f,
            OrganicImmunityTier.Severe    => 0.30f,
            OrganicImmunityTier.Critical  => 0.50f,
            _ => 0f,
        };

        if (chance <= 0f || !_random.Prob(chance))
            return;

        ApplyRandomSymptom(ent);
    }


    private void ApplyRandomSymptom(Entity<OrganicImmunityComponent> ent)
    {

    }


}

