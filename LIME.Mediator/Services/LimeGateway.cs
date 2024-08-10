using LIME.Mediator.Configuration;
using LIME.Shared.Network;

using Microsoft.Extensions.Configuration;
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

    public LimeGateway(LimeMediatorConfig config, ILogger<LimeGateway> logger)
    {
        this.logger = logger;

        _listener = new TcpListener(config.MediatorBindAddress, config.MediatorListenPort);
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
            _ = HandleAcceptConnectionAsync(client);
        }
    }

    private async Task DisconnectAsync(TcpClient client, string message)
    {
        if (client is null)
        {
            return;
        }

        var stream = client.GetStream();

        var packet = new LimePacket(LimePacketType.SMSG_DISCONNECT);
        packet.Data = Encoding.UTF8.GetBytes(message);

        var build = packet.Build();
        await stream.WriteAsync(build);

        client.Close();
    }

    private async Task HandleAcceptConnectionAsync(TcpClient client)
    {
        logger.LogInformation($"Client '{client.Client.RemoteEndPoint}' connected.");

        var endpoint = client.Client.RemoteEndPoint as IPEndPoint;
        if (endpoint is null)
        {
            await DisconnectAsync(client, "An internal error occured.");
            return;
        }
    }
}
