using LIME.Mediator.Network.Packets;
using LIME.Shared.Network;

using System.Net.Security;
using System.Net.Sockets;

namespace LIME.Mediator.Network;

public class LimeClient
{
    public required Guid Guid { get; set; }
    public required LimeClientState State { get; set; }

    public DateTimeOffset LastHeartbeat { get; set; }

    public TcpClient Socket { get; set; }
    public SslStream Stream { get; set; }

    public LimeClient(TcpClient client, SslStream stream)
    {
        Socket = client;
        Stream = stream;
    }

    public async Task SendPacketAsync(ILimePacket packet)
    {
        if (Stream is null || !Stream.CanWrite)
        {
            return;
        }

        await Stream.WriteAsync(packet.Serialize());
    }

    public async Task DisconnectAsync(string message = "")
    {
        try
        {
            var packet = new DisconnectPacket(message);
            await Stream.WriteAsync(packet.Serialize());
        }
        catch (Exception)
        {

        }
        finally
        {
            Socket.Close();
            State = LimeClientState.Disconnected;
        }
    }
}
