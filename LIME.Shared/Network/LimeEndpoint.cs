namespace LIME.Shared.Network;

/// <summary>
/// Represents a network endpoint with an IP address and port number.
/// </summary>
public class LimeEndpoint
{
    public string IPAddress { get; set; }
    public int Port { get; set; }

    public LimeEndpoint(string ipAddress, int port)
    {
        IPAddress = ipAddress;
        Port = port;
    }

    /// <summary>
    /// Returns a string representation of the endpoint in the format "IPAddress:Port".
    /// </summary>
    /// <returns>A string that represents the endpoint.</returns>
    public override string ToString()
    {
        return $"{IPAddress.ToString()}:{Port.ToString()}";
    }
}
