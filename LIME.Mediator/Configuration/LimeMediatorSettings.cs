﻿using LIME.Shared.Network;

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

        RootCertificate = new LimeCertificateSettings("LIME Root", "LIME Root");
        IntermediateCertificate = new LimeCertificateSettings("LIME Root", "LIME Intermediate");
        ServerCertificate = new LimeCertificateSettings("LIME Intermediate", "LIME Mediator");
    }
}
