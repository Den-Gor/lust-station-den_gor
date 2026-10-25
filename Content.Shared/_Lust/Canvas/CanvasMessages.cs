using Lidgren.Network;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Lust.Canvas;

[Serializable, NetSerializable]
public enum CanvasUiKey : byte
{
    Key,
    View,
}

[Serializable, NetSerializable]
public sealed class CanvasOpenViewMessage : BoundUserInterfaceMessage;

public sealed class CanvasSaveMessage : NetMessage
{
    /// <summary>Максимальный размер загружаемого изображения.</summary>
    public const int MaxDataLength = 256 * 1024;

    public override MsgGroups MsgGroup => MsgGroups.String;

    public NetEntity PaperUid;
    public byte[] Data = Array.Empty<byte>();
    public bool IsValid = true;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        IsValid = false;

        if (buffer.LengthBits - buffer.Position < 64)
            return;

        PaperUid = buffer.ReadNetEntity();
        var length = buffer.ReadInt32();

        var remainingBits = buffer.LengthBits - buffer.Position;
        if (length <= 0 || length > MaxDataLength || (long) length * 8 != remainingBits)
            return;

        Data = buffer.ReadBytes(length);
        IsValid = true;
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(PaperUid);
        buffer.Write(Data.Length);
        buffer.Write(Data);
    }
}
