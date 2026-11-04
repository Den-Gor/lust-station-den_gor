using Robust.Shared.GameStates;

namespace Content.Shared._Lust.Multihit.Components;

/// <summary>
/// Компонент маркер защищающий от рекурсии, при повторных ударов а также хронящий
/// Multiplier для последующего уменьшения урона всем повторным ударам
/// </summary>
[RegisterComponent,NetworkedComponent,AutoGenerateComponentState]
public sealed partial class ActiveMeleeMultihitComponent : Component
{
    [DataField,AutoNetworkedField]
    public float Multiplier = 1.0f;
}
