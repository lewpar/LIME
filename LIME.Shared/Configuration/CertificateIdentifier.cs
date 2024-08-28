namespace LIME.Shared.Configuration;

public class CertificateIdentifier
{
    public string Issuer { get; set; }
    public string Thumbprint { get; set; }

    public CertificateIdentifier(string issuer, string thumbprint)
    {
        Issuer = issuer;
        Thumbprint = thumbprint;
    }
}
