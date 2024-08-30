namespace LIME.Shared.Network;

public class LimeEndpoint
{
    public string IPAddress { get; private set; }
    public int Port { get; private set; }

    public LimeEndpoint(string ipAddress, int port)
    {
        IPAddress = ipAddress;
        Port = port;
    }
}
