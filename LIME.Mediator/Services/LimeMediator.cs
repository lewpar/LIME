using LIME.Mediator.Models;

namespace LIME.Mediator.Services;

public class LimeMediator
{
    public List<LimeClient> ConnectedClients { get; set; } = new List<LimeClient>();
}
