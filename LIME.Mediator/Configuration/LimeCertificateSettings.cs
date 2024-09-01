namespace LIME.Mediator.Configuration;

public class LimeCertificateSettings
{
    public string Issuer { get; set; }
    public string Subject { get; set; }

    public string Thumbprint { get; set; }

    public LimeCertificateSettings(string issuer, string subject, string thumbprint = "")
    {
        Issuer = issuer;
        Subject = subject;

        Thumbprint = thumbprint;
    }
}
