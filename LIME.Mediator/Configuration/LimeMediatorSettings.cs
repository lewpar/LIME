using LIME.Shared.Network;

namespace LIME.Mediator.Configuration;

public class LimeMediatorSettings
{
    public LimeEndpoint Listen { get; set; }
    public int HeartbeatSeconds { get; set; }

    public LimeCertificateSettings RootCertificate { get; set; }
    public LimeCertificateSettings IntermediateCertificate { get; set; }
    public LimeCertificateSettings ServerCertificate { get; set; }

    public LimeMediatorSettings()
    {
        Listen = new LimeEndpoint("0.0.0.0", 55123);

        HeartbeatSeconds = 10;

        RootCertificate = new LimeCertificateSettings("Lime", "Lime");
        IntermediateCertificate = new LimeCertificateSettings("Lime", "Lime.Intermediate");
        ServerCertificate = new LimeCertificateSettings("Lime.Intermediate", "Lime.Mediator");
    }
}
