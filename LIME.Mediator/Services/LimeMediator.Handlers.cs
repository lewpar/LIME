using LIME.Mediator.Network;
using LIME.Shared.Extensions;
using LIME.Shared.Network;

namespace LIME.Mediator.Services;

public partial class LimeMediator
{
    private async Task HandleHeartbeatAsync(LimeClient client)
    {
        client.LastHeartbeat = DateTimeOffset.Now;

        logger.LogInformation($"Received heartbeat from '{client.Endpoint}' at '{client.LastHeartbeat}'.");
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
