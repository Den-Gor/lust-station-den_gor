using Robust.Shared.Serialization;

namespace Content.Shared._Lust.Canvas;

[Serializable, NetSerializable]
public enum CanvasUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class CanvasSaveMessage : BoundUserInterfaceMessage
{
    public byte[]? PngData;
    public int BaseRevision;

    public CanvasSaveMessage(byte[] png, int rev) { PngData = png; BaseRevision = rev; }
}

[Serializable, NetSerializable]
public sealed class CanvasClearMessage : BoundUserInterfaceMessage { }
