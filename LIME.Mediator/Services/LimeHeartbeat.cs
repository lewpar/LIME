using LIME.Mediator.Configuration;
using LIME.Mediator.Models;
using LIME.Mediator.Network.Packets;

using System.Diagnostics;

namespace LIME.Mediator.Services;

public class LimeHeartbeat : BackgroundService
{
    private readonly LimeMediator mediator;
    private readonly ILogger<LimeHeartbeat> logger;
    private readonly LimeMediatorConfig config;

    public LimeHeartbeat(LimeMediator mediator, ILogger<LimeHeartbeat> logger, 
        LimeMediatorConfig config)
    {
        this.mediator = mediator;
        this.logger = logger;
        this.config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var stopwatch = new Stopwatch();

        while (!stoppingToken.IsCancellationRequested)
        {
            if (mediator.ConnectedClients.Count == 0)
            {
                continue;
            }

            logger.LogInformation("Sending heartbeat to connected agents..");
            stopwatch.Restart();

            foreach (var agent in mediator.ConnectedClients)
            {
                try
                {
                    var heartbeat = new HeartbeatPacket(new byte[] { 0x01 });
                    await agent.SendPacketAsync(heartbeat);
                }
                catch (Exception ex)
                {
                    logger.LogCritical($"Failed to send heartbeat to agent '{agent.Guid}:{agent.Socket.Client.RemoteEndPoint}': {ex.Message}");
                    logger.LogCritical($"Disconnected '{agent.Guid}:{agent.Socket.Client.RemoteEndPoint}'.");

                    agent.State = LimeClientState.Disconnected;
                }
            }

            stopwatch.Stop();
            logger.LogInformation($"Completed heartbeats in {stopwatch.ElapsedMilliseconds}ms.");

            mediator.ConnectedClients.RemoveAll(c => c.State == LimeClientState.Disconnected);

            await Task.Delay(config.Mediator.HeartbeatSeconds * 1000);
        }
    }
}
