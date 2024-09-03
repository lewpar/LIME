namespace LIME.Mediator.Configuration;

public class LimeCertificateSettings
{
    public string Issuer { get; set; }
    public string Subject { get; set; }

    public string DNS { get; set; }
    public string Thumbprint { get; set; }

    public LimeCertificateSettings(string issuer, string subject, string dns = "", string thumbprint = "")
    {
        Issuer = issuer;
        Subject = subject;

        DNS = dns;
        Thumbprint = thumbprint;
    }
}
