using System.Net;

namespace LIME.Mediator.Configuration;

public class LimeMediatorConfig
{
    public IPAddress MediatorBindAddress { get; set; }
    public int MediatorListenPort { get; set; }

    public LimeMediatorConfig()
    {
        MediatorBindAddress = IPAddress.Parse("0.0.0.0");
        MediatorListenPort = 55123;
    }
}
