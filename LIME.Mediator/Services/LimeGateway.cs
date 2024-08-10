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
    private TcpListener? _listener;
    private readonly ILogger<LimeGateway> logger;

    public LimeGateway(IConfiguration config, ILogger<LimeGateway> logger)
    {
        this.logger = logger;

        ConfigureMediator(config);
    }

    private void ConfigureMediator(IConfiguration config)
    {
        var portEntry = config["LIME:MediatorListenPort"];
        if (string.IsNullOrWhiteSpace(portEntry))
        {
            logger.LogCritical("No port found for MediatorListenPort in appsettings.json.");
            return;
        }

        if (!int.TryParse(portEntry, out var port))
        {
            logger.LogCritical("Invalid port found for MediatorListenPort in appsettings.json.");
            return;
        }

        var endpointEntry = config["LIME:MediatorEndpoint"];
        if (string.IsNullOrWhiteSpace(portEntry))
        {
            logger.LogCritical("No endpoint found for MediatorEndpoint in appsettings.json.");
            return;
        }

        if (!IPAddress.TryParse(endpointEntry, out var endpoint))
        {
            logger.LogCritical("Invalid endpoint found for MediatorEndpoint in appsettings.json.");
            return;
        }

        _listener = new TcpListener(endpoint, port);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_listener is null)
        {
            logger.LogCritical("LimeMediator listener failed to initialize.");
            return;
        }

        _listener.Start();

        await AcceptConnectionsAsync(stoppingToken);
    }

    private async Task AcceptConnectionsAsync(CancellationToken stoppingToken)
    {
        if (_listener is null)
        {
            logger.LogCritical("LimeMediator listener failed to initialize.");
            return;
        }

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
