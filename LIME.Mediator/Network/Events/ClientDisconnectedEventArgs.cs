namespace LIME.Mediator.Network.Events;

public class ClientDisconnectedEventArgs
{
    public LimeClient Client { get; }
    public LimeDisconnectReason Reason { get; }

    public ClientDisconnectedEventArgs(LimeClient client, LimeDisconnectReason reason)
    {
        Client = client;
        Reason = reason;
    }
}
