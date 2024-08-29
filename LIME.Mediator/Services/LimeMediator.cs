using LIME.Mediator.Models;

using LIME.Shared.Extensions;
using LIME.Shared.Network;

namespace LIME.Mediator.Services;

public partial class LimeMediator
{
    public List<LimeClient> ConnectedClients { get; set; } = new List<LimeClient>();

    private Dictionary<LimePacketType, Func<LimeClient, Task>> packetHandlers;
    private readonly ILogger<LimeMediator> logger;

    public LimeMediator(ILogger<LimeMediator> logger)
    {
        packetHandlers = new Dictionary<LimePacketType, Func<LimeClient, Task>>()
        {
        };

        this.logger = logger;
    }

    public async Task ListenForDataAsync(LimeClient client)
    {
        try
        {
            while (true)
            {
                var packetType = await client.Stream.ReadPacketTypeAsync();

                if (!packetHandlers.ContainsKey(packetType))
                {
                    logger.LogWarning($"Client '{client.Socket.Client.RemoteEndPoint}' sent unknown packet type '{packetType}', disconnecting..");
                    await client.DisconnectAsync("Invalid packet.");
                    return;
                }

                var handler = packetHandlers[packetType];

                await handler.Invoke(client);
            }
        }
        catch (Exception ex)
        {
            logger.LogCritical($"{ex.Message}: {ex.StackTrace}");
        }
    }
}
