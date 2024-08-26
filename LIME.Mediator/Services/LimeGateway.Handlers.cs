using LIME.Mediator.Models;
using LIME.Mediator.Network;
using LIME.Shared.Crypto;
using LIME.Shared.Extensions;

using Microsoft.Extensions.Logging;

using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace LIME.Mediator.Services;

public partial class LimeGateway
{
    public async Task SendHandshakeAsync(LimeClient client, SslStream stream)
    {
        if(client.PublicKey is null)
        {
            return;
        }

        using var rsa = new RSACryptoServiceProvider(RSAKeypair.KEY_SIZE);
        rsa.FromXmlString(client.PublicKey.FromBase64());

        if(rsa is null)
        {
            logger.LogCritical($"Could not get RSA Public Key from '{client.Socket.RemoteEndPoint}' key.");
            return;
        }

        var data = Encoding.UTF8.GetBytes(client.Guid.ToString());
        var encryptedData = rsa.Encrypt(data, false);

        var handshake = new HandshakePacket(encryptedData);
        await stream.WriteBytesAsync(handshake.Serialize());

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
