using System.Text;

namespace LIME.Shared.Network.Agent;

public class HandshakePacket : ILimePacket
{
    private readonly string agentGuid;

    public HandshakePacket(string agentGuid)
    {
        this.agentGuid = agentGuid;
    }

    public byte[] Serialize()
    {
        var data = Encoding.UTF8.GetBytes(agentGuid);
        var ms = new MemoryStream();

        ms.Write(BitConverter.GetBytes((int)LimePacketType.CMSG_HANDSHAKE));
        ms.Write(BitConverter.GetBytes(data.Length));
        ms.Write(data);

        return ms.ToArray();
    }
}
