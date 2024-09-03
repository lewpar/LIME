using LIME.Mediator.Configuration;
using LIME.Mediator.Models;
using LIME.Mediator.Network.Packets;

using System.Diagnostics;
using System.Timers;

namespace LIME.Mediator.Services;

public class LimeHeartbeat : BackgroundService
{
    private readonly LimeMediator mediator;
    private readonly ILogger<LimeHeartbeat> logger;

    private System.Timers.Timer timer;

    public LimeHeartbeat(LimeMediator mediator, ILogger<LimeHeartbeat> logger, 
        LimeMediatorConfig config)
    {
        this.mediator = mediator;
        this.logger = logger;

        timer = new System.Timers.Timer(config.Mediator.HeartbeatSeconds * 1000);
        timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting heartbeat monitor..");
        timer.Start();
    }

    private async void OnTimedEvent(Object? source, ElapsedEventArgs e)
    {
        await SendHeartbeatsAsync();
    }

    private async Task SendHeartbeatsAsync()
    {
        var stopwatch = new Stopwatch();

        if (mediator.ConnectedClients.Count == 0)
        {
            return;
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
    }
}
