using LIME.Agent.Network.Packets;
using LIME.Shared.Diagnostics;
using LIME.Shared.Network;

using Microsoft.Extensions.Logging;

namespace LIME.Agent.Services.Tasks;

public class StatisticsTask : ILimeTask
{
    public async Task ExecuteAsync(TaskContext context)
    {
        var stream = context.Stream;

        var memory = await PerformanceMonitor.MeasureMemoryAsync();

        var packet = new StatisticPacket(LimeStatistic.RAM, memory.Min, memory.Max, memory.Current);
        await stream.WriteAsync(packet.Serialize());
    }
}
