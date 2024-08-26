using LIME.Mediator.Network;

using LIME.Shared.Network;

using System.Net.Security;
using System.Net.Sockets;

namespace LIME.Mediator.Models;

public class LimeClient
{
    public required LimeClientState State { get; set; }

    public Socket Socket { get; set; }
    public required SslStream Stream { get; set; }

    public required Guid Guid { get; set; }

    public string? PublicKey { get; set; }

    public LimeClient(TcpClient client) 
    {
        this.Socket = client.Client;
    }

    public async Task SendPacketAsync(ILimePacket packet)
    {
        if(Stream is null || !Stream.CanWrite)
        {
            return;
        }

        await Stream.WriteAsync(packet.Serialize());
    }

    public async Task DisconnectAsync(string message = "")
    {
        var packet = new DisconnectPacket(message);
        await Stream.WriteAsync(packet.Serialize());

        Socket.Close();

        State = LimeClientState.Disconnected;
    }
}
