using System.Text;
using LIME.Shared.Network;

namespace LIME.Mediator.Network.Packets;

public class DisconnectPacket : ILimePacket
{
    private readonly string message;

    public DisconnectPacket(string message = "")
    {
        this.message = message;
    }

    public byte[] Serialize()
    {
        var data = string.IsNullOrWhiteSpace(message) ? new byte[] { 0x01 } : Encoding.UTF8.GetBytes(message);
        var ms = new MemoryStream();

        ms.Write(BitConverter.GetBytes((int)LimePacketType.SMSG_DISCONNECT));
        ms.Write(BitConverter.GetBytes(data.Length));
        ms.Write(data);

        return ms.ToArray();
    }
}
