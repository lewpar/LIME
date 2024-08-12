using LIME.Mediator.Configuration;
using LIME.Mediator.Models;
using LIME.Shared.Network;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LIME.Mediator.Services;

public class LimeGateway : BackgroundService
{
    private TcpListener _listener;
    private readonly ILogger<LimeGateway> logger;

    private Dictionary<LimePacketType, Func<LimeClient, NetworkStream, Task>> packetHandlers;

    public LimeGateway(LimeMediatorConfig config, ILogger<LimeGateway> logger)
    {
        this.logger = logger;

        _listener = new TcpListener(config.MediatorBindAddress, config.MediatorListenPort);

        packetHandlers = new Dictionary<LimePacketType, Func<LimeClient, NetworkStream, Task>>()
        {
            { LimePacketType.CMSG_HANDSHAKE, LimeNetworkHandlers.HandleHandshakeAsync }
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _listener.Start();

        await AcceptConnectionsAsync(stoppingToken);
    }

    private async Task AcceptConnectionsAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var client = await _listener.AcceptTcpClientAsync();

            var limeClient = new LimeClient()
            {
                Guid = Guid.NewGuid(),
                Socket = client.Client,
                Stream = client.GetStream(),
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

        logger.LogInformation($"Client '{client.Socket.RemoteEndPoint}' connected.");

        client.State = LimeClientState.Handshaking;

        await SendHandshakeAsync(client);
        await ListenForDataAsync(client);
    }

    private async Task SendHandshakeAsync(LimeClient client)
    {
        var handshakePacket = new LimePacket(LimePacketType.SMSG_HANDSHAKE);
        handshakePacket.Data = Encoding.UTF8.GetBytes(client.Guid.ToString());

        await client.SendPacketAsync(handshakePacket);
    }

    private async Task ListenForDataAsync(LimeClient client)
    {
        while(true)
        {
            var packetType = await LimeNetwork.ReadPacketTypeAsync(client.Stream);
            
            if(!packetHandlers.ContainsKey(packetType))
            {
                await client.DisconnectAsync($"Client '{client.Socket.RemoteEndPoint}' sent unknown packet type '{packetType}', disconnecting..");
                return;
            }

            var handler = packetHandlers[packetType];
            await handler.Invoke(client, client.Stream);
        }
    }
}
