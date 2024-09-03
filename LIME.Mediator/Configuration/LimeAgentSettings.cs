namespace LIME.Mediator.Configuration;

public class LimeAgentSettings
{
    public LimeCertificateSettings Certificate { get; set; }

    public LimeAgentSettings()
    {
        Certificate = new LimeCertificateSettings("LIME Intermediate", "LIME Agent");
    }
}
