using LIME.Mediator.Network;

namespace LIME.Mediator.Services;

public partial class LimeMediator
{
    private async Task HandleHeartbeatAsync(LimeClient client)
    {
        client.LastHeartbeat = DateTimeOffset.Now;
    }
}
