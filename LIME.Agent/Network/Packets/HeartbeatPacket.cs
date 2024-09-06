using LIME.Shared.Network;

namespace LIME.Agent.Network.Packets;

public class HeartbeatPacket : ILimePacket
{
    public byte[] Serialize()
    {
        var ms = new MemoryStream();

        ms.Write(BitConverter.GetBytes((int)LimeOpCodes.CMSG_HEARTBEAT));

        return ms.ToArray();
    }
}
