using LIME.Mediator.Models;
using LIME.Mediator.Network;

using LIME.Shared.Extensions;
using LIME.Shared.Network;

using Microsoft.Extensions.Hosting;

using System.Security.Cryptography;

namespace LIME.Mediator.Services;

internal class LimeHeartbeat : BackgroundService
{
    private readonly LimeMediator mediator;

    public LimeHeartbeat(LimeMediator mediator)
    {
        this.mediator = mediator;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(10000);

            var tasks = mediator.ConnectedClients.Select(c => SendHeartbeatAsync(c));
            await Task.WhenAll(tasks);
        }
    }

    private async Task SendHeartbeatAsync(LimeClient client)
    {
        try
        {
            var msg = RandomNumberGenerator.GetBytes(4);
            await client.SendPacketAsync(new HeartbeatPacket(msg));

            var responseTask = client.Stream.ReadPacketTypeAsync();

            if(await Task.WhenAny(responseTask, Task.Delay(15000)) == responseTask)
            {
                var responsePacket = await responseTask;

                if(responsePacket != LimePacketType.CMSG_HEARTBEAT)
                {
                    await SendDisconnectAsync(client);
                    return;
                }

                var responseLen = await client.Stream.ReadIntAsync();
                var responseData = await client.Stream.ReadBytesAsync(responseLen);

                if(responseData is null || !responseData.SequenceEqual(msg))
                {
                    await SendDisconnectAsync(client);
                    return;
                }

                // Heartbeat passed, client is ok.

                return;
            }
            else
            {
                // Respond too slow
                await SendDisconnectAsync(client);
            }
        }
        catch(Exception)
        {
            await SendDisconnectAsync(client);
        }
    }

    private async Task SendDisconnectAsync(LimeClient client)
    {
        if(client.Socket is null)
        {
            return;
        }

        await client.SendPacketAsync(new DisconnectPacket());
        client.Socket.Close();
    }
}
