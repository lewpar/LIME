using LIME.Models.Mediator;

using System.Net;
using System.Net.Sockets;

namespace LIME.Services;

public class LimeMediatorGateway : BackgroundService
{
    private TcpListener? _listener;
    private readonly ILogger<LimeMediatorGateway> logger;
    private readonly LimeMediator mediator;

    public LimeMediatorGateway(IConfiguration config, ILogger<LimeMediatorGateway> logger,
                                LimeMediator mediator)
    {
        this.logger = logger;
        this.mediator = mediator;

        ConfigureMediator(config);
    }

    private void ConfigureMediator(IConfiguration config)
    {
        var portEntry = config["LIME:MediatorListenPort"];
        if(string.IsNullOrWhiteSpace(portEntry))
        {
            logger.LogCritical("No port found for MediatorListenPort in appsettings.json.");
            return;
        }

        if(!int.TryParse(portEntry, out var port))
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

        _ = Task.Run(() => AcceptConnectionsAsync(stoppingToken));
        _ = Task.Run(() => SendHeartbeatsAsync(stoppingToken));
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
            // Accept new connections
            await AcceptConnectionAsync(await _listener.AcceptTcpClientAsync());
        }
    }

    private async Task SendHeartbeatsAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Heartbeat connected clients
            await mediator.SendHeartbeatsAsync();
        }
    }

    private async Task AcceptConnectionAsync(TcpClient client)
    {
        logger.LogInformation($"Client '{client.Client.RemoteEndPoint}' connected.");

        mediator.ConnectedAgents.Add(new MediatorClient()
        {
            Client = client
        });
    }
}
