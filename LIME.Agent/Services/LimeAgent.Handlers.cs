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

        Disconnect();
    }

    private async Task HandleJobAsync(SslStream stream)
    {
        var jobType = await stream.ReadEnumAsync<JobType>();
        if(jobType is null)
        {
            logger.LogCritical("Failed to read job type from stream, disconnecting..");

            Disconnect();
            return;
        }

        taskQueue.Enqueue(new JobContext()
        {
            Type = jobType.Value,
            Stream = stream
        });
    }
}
