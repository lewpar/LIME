using LIME.Mediator.Configuration;
using LIME.Mediator.Models;
using LIME.Mediator.Network;

using LIME.Shared.Extensions;
using LIME.Shared.Network;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace LIME.Mediator.Services;

public partial class LimeGateway : BackgroundService
{
    private TcpListener _listener;
    private readonly ILogger<LimeGateway> logger;

    private Dictionary<LimePacketType, Func<LimeClient, SslStream, Task>> packetHandlers;

    public LimeGateway(LimeMediatorConfig config, ILogger<LimeGateway> logger)
    {
        this.logger = logger;

        _listener = new TcpListener(config.MediatorBindAddress, config.MediatorListenPort);

        packetHandlers = new Dictionary<LimePacketType, Func<LimeClient, SslStream, Task>>()
        {
            { LimePacketType.CMSG_HANDSHAKE, HandleHandshakeAsync }
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _listener.Start();

        logger.LogInformation("Listener started, waiting for agents to connect..");

        await AcceptConnectionsAsync(stoppingToken);
    }

    private bool ValidateClientCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
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

    private async Task AcceptConnectionsAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var client = await _listener.AcceptTcpClientAsync();

            var sslStream = new SslStream(client.GetStream(), false, ValidateClientCertificate);

            await sslStream.AuthenticateAsServerAsync(new X509Certificate2("certificate.pfx", ""), true, SslProtocols.Tls13, true);

            var limeClient = new LimeClient()
            {
                Guid = Guid.NewGuid(),
                Socket = client.Client,
                Stream = sslStream,
                State = LimeClientState.Connecting
            };

            _ = HandleAcceptConnectionAsync(limeClient);
        }
    }

    private async Task HandleAcceptConnectionAsync(LimeClient client)
    {
        var endpoint = client.Socket.RemoteEndPoint as IPEndPoint;
        if (endpoint is null)
        {
            await client.DisconnectAsync("An internal error occured.");
            return;
        }

        logger.LogInformation($"Client '{client.Socket.RemoteEndPoint}' connected, starting handshake.");

        client.State = LimeClientState.Handshaking;

        await SendHandshakeAsync(client);
        await ListenForDataAsync(client);
    }

    private async Task SendHandshakeAsync(LimeClient client)
    {
        await client.SendPacketAsync(new HandshakePacket(client.Guid.ToString()));

        logger.LogInformation($"Sent handshake to client {client.Socket.RemoteEndPoint}.");
    }

    private async Task ListenForDataAsync(LimeClient client)
    {
        while(true)
        {
            var packetType = await client.Stream.ReadPacketTypeAsync();
            
            if(!packetHandlers.ContainsKey(packetType))
            {
                logger.LogWarning($"Client '{client.Socket.RemoteEndPoint}' sent unknown packet type '{packetType}', disconnecting..");
                await client.DisconnectAsync("Invalid packet.");
                return;
            }

            var handler = packetHandlers[packetType];

            await handler.Invoke(client, client.Stream);
        }
    }
}
