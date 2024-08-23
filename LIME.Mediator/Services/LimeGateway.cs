using LIME.Mediator.Configuration;
using LIME.Mediator.Models;
using LIME.Mediator.Network;

using LIME.Shared.Crypto;
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

        _listener = new TcpListener(IPAddress.Parse(config.MediatorBindAddress), config.MediatorListenPort);

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

    private async Task AcceptConnectionsAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var client = await _listener.AcceptTcpClientAsync();
            _ = HandleAcceptConnectionAsync(client);
        }
    }

    private async Task HandleAcceptConnectionAsync(TcpClient client)
    {
        var limeClient = new LimeClient()
        {
            Guid = Guid.NewGuid(),
            Socket = client.Client,
            Stream = new SslStream(client.GetStream(), false),
            State = LimeClientState.Connecting
        };

        if(!await AuthenticateAsync(limeClient))
        {
            await limeClient.DisconnectAsync("Failed authentication.");
            return;
        }


        var endpoint = limeClient.Socket.RemoteEndPoint as IPEndPoint;
        if (endpoint is null)
        {
            await limeClient.DisconnectAsync("An internal error occured.");
            return;
        }

        logger.LogInformation($"Client '{limeClient.Socket.RemoteEndPoint}' connected, starting handshake.");

        limeClient.State = LimeClientState.Handshaking;

        await SendHandshakeAsync(limeClient);
        await ListenForDataAsync(limeClient);
    }

    private async Task<bool> AuthenticateAsync(LimeClient client)
    {
        try
        {
            X509Certificate2? cert = LimeCertificate.GetCertificate("localhost");
            if(cert is null)
            {
                logger.LogCritical($"Failed to authenticate client '{client.Socket.RemoteEndPoint}': No valid certificate was found in My store for CurrentUser.");
                return false;
            }

            await client.Stream.AuthenticateAsServerAsync(cert, false, SslProtocols.Tls13, true);

            return true;
        }
        catch(Exception ex)
        {
            logger.LogCritical($"Failed to authenticate client '{client.Socket.RemoteEndPoint}': {ex.Message}");
            return false;
        }
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
