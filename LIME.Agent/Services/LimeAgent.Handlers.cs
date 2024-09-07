using LIME.Agent.Network.Packets;

using LIME.Shared.Diagnostics;
using LIME.Shared.Extensions;
using LIME.Shared.Network;

using Microsoft.Extensions.Logging;

using System.Net.Security;
using System.Text;
using System.Timers;

namespace LIME.Agent.Services;

public partial class LimeAgent
{
    private async void HandleHeartbeatAsync(object? sender, ElapsedEventArgs e)
    {
        try
        {
            if (!connected || stream is null)
            {
                return;
            }

            logger.LogInformation("Sending heartbeat..");

            var packet = new HeartbeatPacket();
            await stream.WriteAsync(packet.Serialize());

            logger.LogInformation("Sent heartbeat.");

            await SendStatisticsAsync(stream);
        }
        catch (Exception ex)
        {
            logger.LogCritical($"{ex.Message}: {ex.StackTrace}");
        }
    }

    private async Task SendStatisticsAsync(SslStream stream)
    {
        logger.LogInformation("Sending statistics..");

        var memory = await PerformanceMonitor.MeasureMemoryAsync();

        var packet = new StatisticPacket(LimeStatistic.RAM, memory.Min, memory.Max, memory.Current);
        await stream.WriteAsync(packet.Serialize());

        logger.LogInformation("Sent statistics.");
    }

    private async Task HandleDisconnectAsync(SslStream stream)
    {
        var dataLength = await stream.ReadIntAsync();
        var data = await stream.ReadBytesAsync(dataLength);

        logger.LogInformation($"Disconnected: {Encoding.UTF8.GetString(data)}");

        connected = false;
    }
}
