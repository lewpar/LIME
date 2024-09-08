using LIME.Agent.Network.Packets;

using LIME.Shared.Diagnostics;
using LIME.Shared.Extensions;
using LIME.Shared.Models;
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

        await DisconnectAsync();
    }

    private async Task HandleTaskAsync(SslStream stream)
    {
        var taskType = await stream.ReadEnumAsync<LimeTaskType>();
        if(taskType is null)
        {
            logger.LogCritical("Failed to read task type from stream, disconnecting..");

            await DisconnectAsync();
            return;
        }

        if(taskType != LimeTaskType.Execute)
        {
            QueueTask(new LimeTask()
            {
                Type = taskType.Value
            });

            return;
        }

        var argsLen = await stream.ReadIntAsync();
        var argsBuf = await stream.ReadBytesAsync(argsLen);

        if(argsBuf is null)
        {
            logger.LogCritical("Failed to read args from stream, disconnecting..");

            await DisconnectAsync();
            return;
        }

        var args = Encoding.UTF8.GetString(argsBuf);

        QueueTask(new LimeTask()
        {
            Type = taskType.Value,
            Args = args
        });
    }

    private async void ProcessQueueAsync(object? sender, ElapsedEventArgs e)
    {
        try
        {
            if(!connected)
            {
                taskTimer.Stop();
                return;
            }

            await taskSignal.WaitAsync();

            if (tasks.TryDequeue(out LimeTask? task))
            {
                logger.LogInformation($"Executing task '{task.Type}'.");

                await ExecuteTaskAsync(task);
            }
        }
        catch (Exception ex)
        {
            logger.LogCritical($"{ex.Message}: {ex.StackTrace}");
        }
    }

    private async Task ExecuteTaskAsync(LimeTask task)
    {
        switch(task.Type)
        {
            case LimeTaskType.Statistics:
                if(stream is null)
                {
                    logger.LogCritical($"Failed to execute task '{task.Type}': stream is null.");
                    return;
                }

                await SendStatisticsAsync(stream);
                break;
        }
    }
}
