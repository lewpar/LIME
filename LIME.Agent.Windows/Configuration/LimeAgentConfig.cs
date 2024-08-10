using System.Net;

namespace LIME.Agent.Windows.Configuration;

public class LimeAgentConfig
{
    public IPAddress MediatorAddress { get; set; }
    public int MediatorPort { get; set; }

    public LimeAgentConfig()
    {
        MediatorAddress = IPAddress.Parse("127.0.0.1");
        MediatorPort = 55123;
    }
}
