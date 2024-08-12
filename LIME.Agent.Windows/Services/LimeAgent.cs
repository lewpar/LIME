using LIME.Agent.Windows.Configuration;
using LIME.Shared.Network;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System.Net.Sockets;

namespace LIME.Agent.Windows.Services;

internal class LimeAgent : IHostedService
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
            // TODO: Disconnect
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

    private async Task<bool> TryHandshakeAsync()
    {
        try
        {
            var stream = client.GetStream();

            var packetType = await LimeNetwork.ReadPacketTypeAsync(stream);
            if (packetType is not LimePacketType.SMSG_HANDSHAKE)
            {
                logger.LogCritical($"Received unexpected packet type '{packetType}' from server, expected '{LimePacketType.SMSG_HANDSHAKE}'.");
                return false;
            }

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
    }
}
