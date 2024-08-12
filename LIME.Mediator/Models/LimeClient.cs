using LIME.Shared.Network;

using System.Net.Sockets;
using System.Text;

namespace LIME.Mediator.Models;

public class LimeClient
{
    public required LimeClientState State { get; set; }

    public required Socket Socket { get; set; }
    public required NetworkStream Stream { get; set; }

    public required Guid Guid { get; set; }

    public LimeClient() { }

    public async Task SendPacketAsync(LimePacket packet)
    {
        if(Stream is null || !Stream.CanWrite)
        {
            return;
        }

        var data = packet.Build();
        await Stream.WriteAsync(data);
    }

    public async Task DisconnectAsync(string message)
    {
        var packet = new LimePacket(LimePacketType.SMSG_DISCONNECT);
        packet.Data = Encoding.UTF8.GetBytes(message);

        var build = packet.Build();
        await Stream.WriteAsync(build);

        Socket.Close();

        State = LimeClientState.Disconnected;
    }
}
