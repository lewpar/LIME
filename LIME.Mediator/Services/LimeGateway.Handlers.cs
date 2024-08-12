using LIME.Mediator.Models;
using LIME.Shared.Network;

using Microsoft.Extensions.Logging;

using System.Net.Sockets;
using System.Text;

namespace LIME.Mediator.Services;

public partial class LimeGateway
{
    public async Task HandleHandshakeAsync(LimeClient client, NetworkStream stream)
    {
        var length = await stream.ReadIntAsync();
        var data = await stream.ReadBytesAsync(length);
        var message = Encoding.UTF8.GetString(data);

        if(client.Guid.ToString().Equals(message))
        {
            logger.LogWarning($"Client '{client.Socket.RemoteEndPoint}' send invalid handshake message '{message}'.");
            await client.DisconnectAsync("Invalid handshake message.");
            return;
        }

        client.State = LimeClientState.Connected;
    }
}
