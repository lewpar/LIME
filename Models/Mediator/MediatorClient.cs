using LIME.Models.Network;
using System.Net.Sockets;

namespace LIME.Models.Mediator;

public class MediatorClient
{
    public TcpClient? Client { get; set; }

    public async Task SendPacketAsync(LimePacket packet)
    {
        if(Client is null)
        {
            return;
        }

        if(!Client.Connected)
        {
            return;
        }

        var stream = Client.GetStream();
        if(!stream.CanWrite)
        {
            return;
        }

        await stream.WriteAsync(packet.ToData());
    }
}
