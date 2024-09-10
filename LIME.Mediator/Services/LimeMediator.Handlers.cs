using LIME.Mediator.Network;
using LIME.Mediator.Network.Packets;

using LIME.Shared.Extensions;
using LIME.Shared.Models;
using LIME.Shared.Network;

using System.Timers;

namespace LIME.Mediator.Services;

public partial class LimeMediator
{
    private async void HandleHeartbeatAsync(object? sender, ElapsedEventArgs e)
    {
        try
        {
            if (ConnectedClients.Count < 1)
            {
                return;
            }

            var now = DateTimeOffset.Now;

            foreach (var client in ConnectedClients)
            {
                var diff = now - client.LastHeartbeat.AddSeconds(config.Mediator.HeartbeatTimeoutMargin);
                if (diff.TotalSeconds > config.Mediator.HeartbeatTimeout)
                {
                    await DisconnectClientAsync(client, LimeDisconnectReason.Timeout);
                    continue;
                }

                var packet = new JobPacket(JobType.Statistics);

                logger.LogInformation("Sending statistic packet..");
                await client.SendPacketAsync(packet);
            }

            ConnectedClients.RemoveAll(client => client.State == LimeClientState.Disconnected);
        }
        catch (Exception ex)
        {
            logger.LogCritical($"{ex.Message}: {ex.StackTrace}");
        }
    }

    private async Task HandleHeartbeatAsync(LimeClient client)
    {
        await Task.Delay(1);

        client.LastHeartbeat = DateTimeOffset.Now;

        logger.LogInformation($"Received heartbeat from '{client.Endpoint.ToString()}' at '{client.LastHeartbeat}'.");
    }

    private async Task HandleStatisticAsync(LimeClient client)
    {
        var stream = client.Stream;

        var statistic = await stream.ReadEnumAsync<LimeStatistic>();
        if(statistic is null)
        {
            logger.LogCritical("Received invalid LimeStatistic.");
            await DisconnectClientAsync(client);
            return;
        }

        var min = DataUnitConverter.GetDataUnitFromBytes(await stream.ReadLongAsync());
        var max = DataUnitConverter.GetDataUnitFromBytes(await stream.ReadLongAsync());
        var current = DataUnitConverter.GetDataUnitFromBytes(await stream.ReadLongAsync());

        logger.LogInformation($"Got statistic: '{statistic.ToString()}', min: '{min.ToString()}', max: '{max.ToString()}', current: '{current.ToString()}'");
    }
}
