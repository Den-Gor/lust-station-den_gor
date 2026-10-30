using Content.Shared._Lust.Implants.Components;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.Implants;
/// <summary>
///  Таблица оторжения имплантов пороги тиров и симптомы по каждому тиру.
/// </summary>
[Prototype("cyberPsychosisConfig")]
public sealed partial class CyberPsychosisConfigPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Тиры с порогами и симптомами где ключ сам тир а значения его кофиг
    /// </summary>
    [DataField]
    public Dictionary<OrganicImmunityTier, TierConfig> Tiers = new();

    [DataDefinition]
    public sealed partial class TierConfig
    {
        /// <summary>
        /// Порог определяющийся по значению >= от Threshold определяет текущий тир
        /// </summary>
        [DataField(required: true)]
        public float Threshold;

        /// <summary>
        /// Урон накладываемый при данном тире
        /// </summary>
        [DataField]
        public DamageSpecifier DamagePerSecond = new();

        /// <summary>
        /// Список симптомов при тире
        /// </summary>
        [DataField]
        public List<SymptomEntry> Symptoms = new();
    }

    [DataDefinition]
    public sealed partial class SymptomEntry
    {
        /// <summary>
        /// Симптом
        /// </summary>
        [DataField(required: true)]
        public Symptom Symptom;

        /// <summary>
        /// Шанс выподания симптома
        /// </summary>
        [DataField]
        public float Chance = 0.1f;

        /// <summary>
        /// Длительность для симптомов статус эффектов
        /// </summary>
        [DataField]
        public TimeSpan Duration = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Кулдаун на срабатывание
        /// </summary>
        [DataField]
        public TimeSpan Cooldown;

        /// <summary>
        /// Сила кровотичения (Только для Bleed)
        /// </summary>
        [DataField]
        public float BleedAmount = 8f;
    }
}
