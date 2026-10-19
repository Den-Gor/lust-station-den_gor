using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Lust.Canvas;

[Serializable, NetSerializable]
public enum CanvasUiKey : byte
{
    Key,
    View,
}

public sealed class CanvasSaveMessage : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.String;

    public NetEntity PaperUid;
    public byte[] Data = Array.Empty<byte>();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        PaperUid = buffer.ReadNetEntity();
        var length = buffer.ReadInt32();
        Data = buffer.ReadBytes(length);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(PaperUid);
        buffer.Write(Data.Length);
        buffer.Write(Data);
    }
}
