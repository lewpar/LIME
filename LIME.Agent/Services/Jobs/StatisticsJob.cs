using LIME.Agent.Network.Packets;

using LIME.Shared.Diagnostics;
using LIME.Shared.Network;

namespace LIME.Agent.Services.Tasks;

public class StatisticsJob : IJob
{
    public async Task ExecuteAsync(JobContext context)
    {
        var stream = context.Stream;

        var memory = await SystemMonitor.MeasureMemoryAsync();

        var packet = new StatisticPacket(LimeStatistic.RAM, memory.Min, memory.Max, memory.Current);
        await stream.WriteAsync(packet.Serialize());
    }
}
