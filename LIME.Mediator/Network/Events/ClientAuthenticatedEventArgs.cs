using LIME.Mediator.Models;

namespace LIME.Mediator.Network.Events;

public class ClientAuthenticatedEventArgs
{
    public LimeClient Client { get; }

    public ClientAuthenticatedEventArgs(LimeClient client)
    {
        Client = client;
    }
}
