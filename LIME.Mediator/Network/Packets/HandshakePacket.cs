using System.Text;

using LIME.Shared.Network;

namespace LIME.Mediator.Network;

public class HandshakePacket : ILimePacket
{
    private readonly string message;

    public HandshakePacket(string message)
    {
        this.message = message;
    }

    public byte[] Serialize()
    {
        var data = Encoding.UTF8.GetBytes(message);
        var ms = new MemoryStream();

        ms.Write(BitConverter.GetBytes((int)LimePacketType.SMSG_HANDSHAKE));
        ms.Write(BitConverter.GetBytes(data.Length));
        ms.Write(data);

        return ms.ToArray();
    }
}
