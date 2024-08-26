using LIME.Agent.Windows.Configuration;
using LIME.Agent.Windows.Network;
using LIME.Shared.Crypto;
using LIME.Shared.Extensions;
using LIME.Shared.Network;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace LIME.Agent.Windows.Services;

internal partial class LimeAgent : IHostedService
{
    private readonly ILogger<LimeAgent> logger;
    private readonly LimeAgentConfig config;

    private TcpClient client;

    public LimeAgent(ILogger<LimeAgent> logger, LimeAgentConfig config)
    {
        this.logger = logger;
        this.config = config;

        client = new TcpClient();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Lime Agent Started.");

        if(!await TryConnectToMediatorAsync())
        {
            return;
        }

        if(!await TryHandshakeAsync())
        {
            return;
        }
    }

    private async Task<bool> TryConnectToMediatorAsync()
    {
        try
        {
            await client.ConnectAsync(config.MediatorAddress, config.MediatorPort);
        }
        catch(Exception ex)
        {
            logger.LogCritical($"An exception occured while trying to connect to the mediator server at '{config.MediatorAddress.ToString()}:{config.MediatorPort.ToString()}': {ex.Message}");
            return false;
        }

        return true;
    }

    private bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0 &&
            chain is not null)
        {
            foreach (X509ChainStatus status in chain.ChainStatus)
            {
                if (status.Status == X509ChainStatusFlags.UntrustedRoot)
                {
                    return true;
                }
            }

            return false;
        }

        return sslPolicyErrors == SslPolicyErrors.None;
    }

    private async Task<bool> TryHandshakeAsync()
    {
        try
        {
            var stream = new SslStream(client.GetStream(), false, ValidateServerCertificate);
            await stream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions()
            {
                EnabledSslProtocols = SslProtocols.Tls13,
                TargetHost = "LIME"
            });

            var packetType = await stream.ReadPacketTypeAsync();
            if (packetType is not LimePacketType.SMSG_HANDSHAKE)
            {
                logger.LogCritical($"Received unexpected packet type '{packetType}' from server, expected '{LimePacketType.SMSG_HANDSHAKE}'.");
                return false;
            }

            var msgLen = await stream.ReadIntAsync();
            var msgEncrypted = await stream.ReadBytesAsync(msgLen);

            var privateKey = Environment.GetEnvironmentVariable("LIME_AGENT_PRIVATE_KEY", EnvironmentVariableTarget.Process);
            if(string.IsNullOrWhiteSpace(privateKey))
            {
                logger.LogCritical("Failed to get agent private key.");
                return false;
            }

            using var rsa = new RSACryptoServiceProvider(RSAKeypair.KEY_SIZE);
            rsa.FromXmlString(privateKey.FromBase64());
            var msg = Encoding.UTF8.GetString(rsa.Decrypt(msgEncrypted, false));

            logger.LogInformation($"Got message: {msg}");

            var packet = new HandshakePacket(msg);

            await stream.WriteBytesAsync(packet.Serialize());

            logger.LogInformation("Handshake succeeded.");
        }
        catch(Exception ex)
        {
            logger.LogCritical($"An exception occured during handshake: {ex.Message}");
            return false;
        }

        return true;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Lime Agent Stopped.");

        await Task.Delay(1);
    }
}
