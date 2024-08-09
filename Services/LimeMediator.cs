using LIME.Models.Mediator;
using LIME.Models.Network;

using System.Diagnostics;

namespace LIME.Services;

public class LimeMediator
{
    public int HeartbeatFrequency { get; private set; }
    public bool HeartbeatInProgress { get; set; }

    public DateTime LastHeartbeat { get; set; }

    public List<MediatorClient> ConnectedAgents { get; set; } = new List<MediatorClient>();

    private readonly ILogger<LimeMediator> logger;

    public LimeMediator(IConfiguration config, ILogger<LimeMediator> logger)
    {
        this.logger = logger;

        ConfigureMediator(config);
    }

    private void ConfigureMediator(IConfiguration config)
    {
        var heartbeatFrequencyEntry = config["LIME:MediatorHeartbeatFrequency"];
        if (string.IsNullOrWhiteSpace(heartbeatFrequencyEntry))
        {
            logger.LogCritical("No heartbeat frequency found for MediatorHeartbeatFrequency in appsettings.json.");
            return;
        }

        if (!int.TryParse(heartbeatFrequencyEntry, out int heartbeatFrequency))
        {
            logger.LogCritical("Invalid heartbeat frequency for MediatorHeartbeatFrequency in appsettings.json.");
            return;
        }

        HeartbeatFrequency = heartbeatFrequency;
    }

    public async Task SendHeartbeatsAsync()
    {
        if(ConnectedAgents.Count == 0)
        {
            return;
        }

        if(HeartbeatInProgress)
        {
            return;
        }

        var now = DateTime.Now;
        var diff = now - LastHeartbeat;

        if(diff.TotalSeconds < HeartbeatFrequency)
        {
            return;
        }

        logger.LogInformation("Sending heartbeat to connected agents..");

        var sw = new Stopwatch();
        sw.Start();

        HeartbeatInProgress = true;

        foreach (var client in ConnectedAgents.ToList())
        {
            if (!await TryHeartbeatAsync(client))
            {
                logger.LogWarning($"Agent '{client.Socket.RemoteEndPoint}' did not respond to heartbeat. Disconnecting..");
                ConnectedAgents.Remove(client);
            }
        }

        LastHeartbeat = now;
        HeartbeatInProgress = false;

        sw.Stop();

        logger.LogInformation($"Finished sending heartbeat in {sw.ElapsedMilliseconds}ms.");
    }

    private async Task<bool> TryHeartbeatAsync(MediatorClient client)
    {
        try
        {
            var packet = new LimePacket(LimePacketType.Heartbeat);
            packet.Data = new byte[]
            {
                0x01
            };

            await client.SendPacketAsync(packet);
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }
}
