using LIME.Models.Network;

using System.Net.Sockets;

namespace LIME.Models.Mediator;

public class MediatorClient
{
    public bool IsConnected { get; set; }

    public NetworkStream Stream { get; set; }
    public Socket Socket { get; set; }

    public MediatorClient(Socket socket, NetworkStream stream)
    {
        this.Socket = socket;
        this.Stream = stream;
    }

    public async Task SendPacketAsync(LimePacket packet)
    {
        if(!Stream.CanWrite)
        {
            return;
        }

        await Stream.WriteAsync(packet.ToData());
    }
}
