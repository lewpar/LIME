using LIME.Shared.Network;

namespace LIME.Mediator.Network;

public class HeartbeatPacket : ILimePacket
{
    private readonly byte[] message;

    public HeartbeatPacket(byte[] message)
    {
        this.message = message;
    }

    public byte[] Serialize()
    {
        var data = message;
        var ms = new MemoryStream();

        ms.Write(BitConverter.GetBytes((int)LimePacketType.SMSG_HEARTBEAT));
        ms.Write(BitConverter.GetBytes(data.Length));
        ms.Write(data);

        return ms.ToArray();
    }
}
