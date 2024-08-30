﻿using LIME.Shared.Network;

namespace LIME.Mediator.Configuration;

public class LimeDashboardSettings
{
    public LimeEndpoint Listen { get; private set; }
    public LimeCertificateSettings Certificate { get; set; }

    public LimeDashboardSettings()
    {
        Listen = new LimeEndpoint("0.0.0.0", 55124);
        Certificate = new LimeCertificateSettings("Lime.Intermediate", "localhost");
    }
}
