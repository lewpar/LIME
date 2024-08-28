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

    public static X509Certificate2Collection GetCertificates()
    {
        var store = new X509Store(StoreName.My, StoreLocation.CurrentUser, OpenFlags.ReadOnly);
        var certificates = store.Certificates;

        if (certificates is null)
        {
            return new X509Certificate2Collection();
        }

        return certificates;
    }

    public static X509Certificate2 CreateRootCertificate(string issuer)
    {
        using var rsa = RSA.Create(2048);

        var subject = new X500DistinguishedName($"CN={issuer}");

        var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));

        var certificate = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));

        return new X509Certificate2(certificate.Export(X509ContentType.Pfx));
    }

    public static X509Certificate2 CreateIntermediateCertificate(X509Certificate2 issuer)
    {
        using var rsa = RSA.Create(2048);
        var subject = new X500DistinguishedName("CN=LimeIntermediate");

        var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));

        var certificate = request.Create(issuer, DateTimeOffset.Now, issuer.NotAfter, Guid.NewGuid().ToByteArray());

        return new X509Certificate2(certificate.Export(X509ContentType.Pfx));
    }

    public static void StoreCertificate(X509Certificate2 cert)
    {
        var store = new X509Store(StoreName.My, StoreLocation.CurrentUser, OpenFlags.ReadWrite);
        store.Add(cert);
    }

    public static X509Certificate2 GetCertificateFromBase64(string base64)
    {
        return new X509Certificate2(Convert.FromBase64String(base64));
    }
}
