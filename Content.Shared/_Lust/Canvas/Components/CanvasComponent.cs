using Robust.Shared.GameStates;

namespace Content.Shared._Lust.Canvas.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CanvasComponent  : Component
{
    public const int Size = 256;

    [DataField, AutoNetworkedField]
    public string? ImageId;

    [DataField, AutoNetworkedField]
    public int Revision;

    [DataField, AutoNetworkedField]
    public bool HasDrawing;

    [ViewVariables]
    public string? ImagePath =>  ImageId  == null ? null : $"/NetTextures/Canvas/{ImageId}.png";
}
