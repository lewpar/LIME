using LIME.Shared.Network;
using LIME.Shared.Network.Mediator;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace LIME.Mediator.Models;

public class LimeClient
{
    public required LimeClientState State { get; set; }

    public required Socket Socket { get; set; }
    public required SslStream Stream { get; set; }

    public required Guid Guid { get; set; }

    public LimeClient() { }

    public async Task SendPacketAsync(ILimePacket packet)
    {
        if(Stream is null || !Stream.CanWrite)
        {
            return;
        }

        await Stream.WriteAsync(packet.Serialize());
    }

    public async Task DisconnectAsync(string message)
    {
        var packet = new DisconnectPacket(message);
        await Stream.WriteAsync(packet.Serialize());

        Socket.Close();

        State = LimeClientState.Disconnected;
    }
}
