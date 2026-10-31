using Content.Shared._Lust.Implants;
using Content.Shared._Lust.Implants.Components;
using Content.Shared.Body;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Jittering;
using Content.Shared.Medical;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
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
    [Dependency] private EntityQuery<OrganicImplantComponent> _implantQuery = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private SharedJitteringSystem _jitter = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    private static readonly EntProtoId StutterProto = "StatusEffectStutter";
    private static readonly EntProtoId WoozyProto = "StatusEffectWoozy";

    private static readonly ProtoId<CyberPsychosisConfigPrototype> ConfigId = "LustCyberPsychosis";

    public override void Initialize()
    {
        base.Initialize();
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

            if (_mobState.IsDead(uid))
                continue;

            immunity.NextTick = now + TimeSpan.FromSeconds(1);

            if (immunity.DrainDirty)
            {
                RebuildDrain(immunity, body);
                immunity.DrainDirty = false;
            }

            var net = immunity.BaseRegen - immunity.CachedDrain;
            var newCurrent = Math.Clamp(immunity.Current + net, 0f, immunity.Max);

            if (MathF.Abs(newCurrent - immunity.Current) >= 0.0001f)
            {
                immunity.Current = newCurrent;
                var oldTier = immunity.CurrentTier;
                UpdateTier(immunity);
                if (immunity.CurrentTier != oldTier)
                    immunity.SymptomCooldowns.Clear();

                Dirty(uid, immunity);
            }
            // нанесение урона
            ApplyTierDamage(uid, immunity);

            // Каст симптомов
            if (now >= immunity.NextSymptomCheck)
            {
                immunity.NextSymptomCheck = now + TimeSpan.FromSeconds(5);
                TrySymptoms((uid, immunity));
            }
        }
    }

    /// <summary>
    /// Функция нанисение урона, урон берется из прототипа в соответсвии с текщей стадией
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="immunity"></param>
    private void ApplyTierDamage(EntityUid uid, OrganicImmunityComponent immunity)
    {
        if (!_prototypes.TryIndex(ConfigId, out var config))
            return;

        if (!config.Tiers.TryGetValue(immunity.CurrentTier, out var tierConfig))
            return;

        if (tierConfig.DamagePerSecond.Empty)
            return;

        _damageable.TryChangeDamage(
            uid,
            tierConfig.DamagePerSecond,
            ignoreResistances: true,
            interruptsDoAfters: false);
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
        if (!_prototypes.TryIndex(ConfigId, out var config))
        {
            immunity.CurrentTier = OrganicImmunityTier.Critical;
            return;
        }

        var bestThreshold = float.MinValue;
        var result = OrganicImmunityTier.Critical;

        foreach (var (tier, cfg) in config.Tiers)
        {
            if (cfg.Threshold <= immunity.Current && cfg.Threshold > bestThreshold)
            {
                bestThreshold = cfg.Threshold;
                result = tier;
            }
        }

        immunity.CurrentTier = result;
    }

    /// <summary>
    /// Функция попытка применить симптом на игрока
    /// </summary>
    /// <param name="ent"></param>
    private void TrySymptoms(Entity<OrganicImmunityComponent> ent)
    {
        if (!_prototypes.TryIndex(ConfigId, out var config))
            return;

        if (!config.Tiers.TryGetValue(ent.Comp.CurrentTier, out var tierConfig))
            return;

        if (tierConfig.Symptoms.Count == 0)
            return;

        foreach (var entry in tierConfig.Symptoms)
        {
            if (entry.Chance <= 0f)
                continue;

            if (!_random.Prob(entry.Chance))
                continue;

            if (entry.Cooldown > TimeSpan.Zero && IsOnCooldown(ent, entry.Symptom))
                continue;

            ApplySymptom(ent, entry);

            if (entry.Cooldown > TimeSpan.Zero)
                SetCooldown(ent, entry.Symptom, entry.Cooldown);
        }
    }
    private bool IsOnCooldown(Entity<OrganicImmunityComponent> ent, Symptom symptom)
        => ent.Comp.SymptomCooldowns.TryGetValue(symptom, out var until)
           && _timing.CurTime < until;

    private void SetCooldown(Entity<OrganicImmunityComponent> ent, Symptom symptom, TimeSpan duration)
        => ent.Comp.SymptomCooldowns[symptom] = _timing.CurTime + duration;

    /// <summary>
    /// Функция накладывает симптом на игрока
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="entry"></param>
    private void ApplySymptom(Entity<OrganicImmunityComponent> ent, CyberPsychosisConfigPrototype.SymptomEntry entry)
    {
        var uid = ent.Owner;

        switch (entry.Symptom)
        {
            case Symptom.Jitter:
                _jitter.DoJitter(uid, entry.Duration, refresh: false);
                break;
            case Symptom.Stutter:
                _status.TryAddStatusEffectDuration(uid, StutterProto, entry.Duration);
                break;
            case Symptom.Vomit:
                _vomit.Vomit(uid);
                break;
            case Symptom.Bleed:
                _bloodstream.TryModifyBleedAmount(uid, entry.BleedAmount);
                break;
            case Symptom.Woozy:
                _status.TryAddStatusEffectDuration(uid, WoozyProto, entry.Duration);
                break;
        }
    }
}

