namespace LIME.Shared.Network;

public class LimeEndpoint
{
    public string IPAddress { get; set; }
    public int Port { get; set; }

    public LimeEndpoint(string ipAddress, int port)
    {
        IPAddress = ipAddress;
        Port = port;
    }
}
