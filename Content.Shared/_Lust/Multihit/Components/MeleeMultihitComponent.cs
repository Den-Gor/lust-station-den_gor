using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._Lust.Multihit.Components;

/// <summary>
/// Компонент добовляемый на оружие чтобы позволить наносить несколько ударов при держании
/// в руках нескольких  оружий имеющий данный компонент, а также тег соответсвующий тегу в вайтлисте компонента
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MeleeMultihitComponent : Component
{
    /// <summary>
    /// Число множителя на сколько последующие удары урезаются
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DamageMultiplier = 0.6f;

    /// <summary>
    /// время задержки между ударами
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(0.25f);

    /// <summary>
    /// число максимально допустимого  комбо ударов
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxExtraHits = 2;

    /// <summary>
    /// Вайтлист
    /// </summary>
    [DataField,ViewVariables(VVAccess.ReadOnly)]
    public EntityWhitelist? WeaponWhitelist;
}
