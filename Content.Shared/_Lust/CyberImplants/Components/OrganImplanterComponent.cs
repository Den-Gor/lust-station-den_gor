using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Lust.CyberImplants.Components;

/// <summary>
/// Компонент предмета имлантера органов.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OrganImplanterComponent : Component
{
    /// <summary>
    /// ID слота в ItemSlotsComponent и ContainerContainer.
    /// </summary>
    public const string SlotId = "organ_slot";

    /// <summary>
    /// Орган, который будет находиться в слоте при старте (задаётся в YAML).
    /// </summary>
    [DataField]
    public EntProtoId? StartingOrgan;

    /// <summary>
    /// Описание слота — как у имплантера, задаётся в YAML внутри компонента.
    /// Регистрируется через ItemSlotsInitEvent.
    /// </summary>
    [DataField(required: true)]
    public ItemSlot Slot = new();

    /// <summary>
    /// Флаг использования имплантер. Если true и не Infinite — имплантер недоступен.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public bool Used;

    /// <summary>
    /// Если true — импнатер не расходуется после использования.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool Infinite;

    /// <summary>
    /// Длительность do-after операции извлечения/вставки органа.
    /// </summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(6);

    /// <summary>
    /// Урон, наносимый цели после успешной операции.
    /// </summary>
    [DataField]
    public DamageSpecifier Damage = new();

    /// <summary>
    /// Количество кровотечения накладываемое на цель после операции.
    /// </summary>
    [DataField]
    public float BleedAmount;

    /// <summary>
    /// Звук  имплантирования
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? StartSound;
}
