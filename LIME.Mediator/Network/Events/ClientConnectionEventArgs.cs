using System.Net.Sockets;

namespace LIME.Mediator.Network.Events;

public class ClientConnectionEventArgs
{
    public TcpClient Client { get; }

    public ClientConnectionEventArgs(TcpClient client)
    {
        Client = client;
    }
}
