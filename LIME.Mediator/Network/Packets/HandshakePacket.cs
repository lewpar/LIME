using LIME.Shared.Network;

namespace LIME.Mediator.Network.Packets;

public class HandshakePacket : ILimePacket
{
    private readonly byte[] message;

    public HandshakePacket(byte[] message)
    {
        this.message = message;
    }

    public byte[] Serialize()
    {
        var ms = new MemoryStream();

        ms.Write(BitConverter.GetBytes((int)LimePacketType.SMSG_HANDSHAKE));
        ms.Write(BitConverter.GetBytes(message.Length));
        ms.Write(message);

        return ms.ToArray();
    }
}
