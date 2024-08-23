using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace LIME.Shared.Crypto;

public class LimeCertificate
{
    public static X509Certificate2? GetCertificate(string thumbprint)
    {
        var store = new X509Store(StoreName.My, StoreLocation.CurrentUser, OpenFlags.ReadOnly);
        var certificates = store.Certificates;

        if(certificates is null)
        {
            return null;
        }

        foreach(var certificate in certificates)
        {
            // Do not get expired certificate
            if(DateTime.Now > certificate.NotAfter)
            {
                continue;
            }

            if(certificate.Thumbprint == thumbprint)
            {
                return certificate;
            }
        }

        return null;
    }

    public static X509Certificate2 CreateCertificate()
    {
        using var rsa = RSA.Create(2048);

        var subject = new X500DistinguishedName("CN=LimeMediator");

        var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        var certificate = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));

        return certificate;
    }

    public static void StoreCertificate(X509Certificate2 cert)
    {
        var store = new X509Store(StoreName.My, StoreLocation.CurrentUser, OpenFlags.ReadWrite);
        store.Add(cert);
    }
}
