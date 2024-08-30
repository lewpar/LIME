namespace LIME.Mediator.Configuration;

public class LimeCertificateSettings
{
    public string Issuer { get; private set; }
    public string Subject { get; private set; }

    public string Thumbprint { get; set; }

    public LimeCertificateSettings(string issuer, string subject, string thumbprint = "")
    {
        Issuer = issuer;
        Subject = subject;

        Thumbprint = thumbprint;
    }
}
