using Robust.Shared.GameStates;

using Robust.Shared.Serialization;

namespace Content.Shared._Lust.Canvas.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CanvasComponent  : Component
{
    public const int Size = 256;

    /// <summary>
    ///  Айди картинки
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? ImageId;

    /// <summary>
    ///  Рисуют ли сейчас
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HasDrawing;

    /// <summary>
    /// Аккаунт автора последнего рисунка
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? AuthorAccount;

    [ViewVariables]
    public string? ImagePath =>  ImageId  == null ? null : $"/NetTextures/Canvas/{ImageId}.png";
}

[Serializable, NetSerializable]
public enum CanvasVisuals : byte
{
    HasDrawing,
}
