namespace LIME.Shared.Configuration;

public class CertificateIdentifier
{
    public string Issuer { get; set; }
    public string Subject { get; set; }
    public string Thumbprint { get; set; }

    public CertificateIdentifier(string issuer, string subject, string thumbprint = "")
    {
        Issuer = issuer;
        Subject = subject;
        Thumbprint = thumbprint;
    }
}
