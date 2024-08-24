using LIME.Mediator.Models;
using LIME.Shared.Extensions;

using Microsoft.Extensions.Logging;

using System.Net.Security;
using System.Text;

namespace LIME.Mediator.Services;

public partial class LimeGateway
{
    public async Task SendHandshakeAsync(LimeClient client, SslStream stream)
    {
        logger.LogInformation($"Sent handshake to client {client.Socket.RemoteEndPoint}.");
    }

    public async Task HandleHandshakeAsync(LimeClient client, SslStream stream)
    {
        var length = await stream.ReadIntAsync();
        var data = await stream.ReadBytesAsync(length);
        var message = Encoding.UTF8.GetString(data);
        var expectedMsg = client.Guid.ToString();

        if(message != expectedMsg)
        {
            logger.LogWarning($"Client '{client.Socket.RemoteEndPoint}' send invalid handshake message '{message}', expected '{expectedMsg}'.");
            await client.DisconnectAsync("Invalid handshake message.");
            return;
        }

        client.State = LimeClientState.Connected;

        logger.LogInformation($"Client {client.Socket.RemoteEndPoint} passed handshake.");
    }
}
