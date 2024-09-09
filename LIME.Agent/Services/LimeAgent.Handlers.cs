using LIME.Agent.Network.Packets;

using LIME.Shared.Extensions;
using LIME.Shared.Models;

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
            taskQueue.Enqueue(new TaskContext()
            {
                Task = new LimeTask()
                {
                    Type = taskType.Value
                },
                Stream = stream
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

        taskQueue.Enqueue(new TaskContext()
        {
            Task = new LimeTask()
            {
                Type = taskType.Value,
                Args = args
            },
            Stream = stream
        });
    }
}
