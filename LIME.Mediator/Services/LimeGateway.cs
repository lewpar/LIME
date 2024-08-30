using LIME.Mediator.Configuration;

using System.Net;

using LIME.Mediator.Network;
using LIME.Mediator.Network.Events;

namespace LIME.Mediator.Services;

public partial class LimeGateway : BackgroundService
{
    private LimeServer server;
    private LimeMediator mediator;
    private readonly ILogger<LimeGateway> logger;

    public LimeGateway(LimeMediatorConfig config, LimeMediator mediator, ILogger<LimeGateway> logger)
    {
        this.mediator = mediator;
        this.logger = logger;

        server = new LimeServer(IPAddress.Parse(config.Mediator.Listen.IPAddress), config.Mediator.Listen.Port, config.Mediator.ServerCertificate.Thumbprint, true);
        server.ServerStarted += Server_ServerStarted;
        server.ClientAuthenticating += Server_ClientAuthenticating;
        server.ClientAuthenticationFailed += Server_ClientAuthenticationFailed;
        server.ClientAuthenticated += Server_ClientAuthenticated;
    }

    private void Server_ClientAuthenticated(object? sender, ClientAuthenticatedEventArgs e)
    {
        logger.LogInformation($"Client '{e.Client.Socket.Client.RemoteEndPoint}' authenticated.");

        mediator.ConnectedClients.Add(e.Client);
    }

    private void Server_ClientAuthenticationFailed(object? sender, ClientAuthenticationFailedEventArgs e)
    {
        logger.LogInformation($"Client '{e.Client.Client.RemoteEndPoint}' failed authentication: {e.Message}");
    }

    private void Server_ClientAuthenticating(object? sender, ClientAuthenticatingEventArgs e)
    {
        logger.LogInformation($"Client '{e.Client.Client.RemoteEndPoint}' has connected, authenticating..");
    }

    private void Server_ServerStarted(object? sender, EventArgs e)
    {
        logger.LogInformation("Started LimeServer, waiting for connections..");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await server.StartAsync(stoppingToken);
    }
}
