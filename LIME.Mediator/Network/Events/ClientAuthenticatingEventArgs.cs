using System.Net.Sockets;

namespace LIME.Mediator.Network.Events;

public class ClientAuthenticatingEventArgs
{
    public TcpClient Client { get; }

    public ClientAuthenticatingEventArgs(TcpClient client)
    {
        Client = client;
    }
}
