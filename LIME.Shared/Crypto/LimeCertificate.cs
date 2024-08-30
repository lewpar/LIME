using System.Data;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace LIME.Shared.Crypto;

public class LimeCertificate
{
    public static X509Certificate2? GetCertificate(string thumbprint, StoreName storeName = StoreName.My)
    {
        var store = new X509Store(storeName, StoreLocation.CurrentUser, OpenFlags.ReadOnly);
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

    public static X509Certificate2Collection GetCertificates(StoreName storeName = StoreName.My)
    {
        var store = new X509Store(storeName, StoreLocation.CurrentUser, OpenFlags.ReadOnly);
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
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | 
                                                                    X509KeyUsageFlags.DigitalSignature | 
                                                                    X509KeyUsageFlags.NonRepudiation, true));

        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection
        {
            new Oid("1.3.6.1.5.5.7.3.2"), // Client Authentication
            new Oid("1.3.6.1.5.5.7.3.1") // Server Authentication
        }, false));

        var certificate = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));

        return new X509Certificate2(certificate.Export(X509ContentType.Pfx));
    }

    public static X509Certificate2 CreateIntermediateCertificate(X509Certificate2 rootCertificate, string subject)
    {
        using var rsa = RSA.Create(2048);

        var subjectName = new X500DistinguishedName($"CN={subject}");

        var request = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign |
                                                                    X509KeyUsageFlags.DigitalSignature |
                                                                    X509KeyUsageFlags.NonRepudiation, true));

        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection
        {
            new Oid("1.3.6.1.5.5.7.3.2"), // Client Authentication
            new Oid("1.3.6.1.5.5.7.3.1") // Server Authentication
        }, false));

        var certificate = request.Create(rootCertificate, DateTimeOffset.Now, rootCertificate.NotAfter, Guid.NewGuid().ToByteArray());

        var certificatePrivate = certificate.CopyWithPrivateKey(rsa);

        return new X509Certificate2(certificatePrivate.Export(X509ContentType.Pkcs12, ""), "", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
    }

    public static X509Certificate2 CreateSignedCertificate(X509Certificate2 issuer, string subject, X509CertificateAuthRole role)
    {
        using var rsa = RSA.Create(2048);

        var subjectName = new X500DistinguishedName($"CN={subject}");

        var request = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyEncipherment | 
                                                                    X509KeyUsageFlags.DigitalSignature | 
                                                                    X509KeyUsageFlags.NonRepudiation, true));

        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection
        {
            role == X509CertificateAuthRole.Client ? new Oid("1.3.6.1.5.5.7.3.2") : new Oid("1.3.6.1.5.5.7.3.1")
        }, false));

        if(role == X509CertificateAuthRole.WebServer)
        {
            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddDnsName(subject);

            request.CertificateExtensions.Add(sanBuilder.Build());
        }

        var certificate = request.Create(issuer, DateTimeOffset.Now, issuer.NotAfter, Guid.NewGuid().ToByteArray());

        var certificatePrivate = certificate.CopyWithPrivateKey(rsa);

        return new X509Certificate2(certificatePrivate.Export(X509ContentType.Pkcs12, ""), "", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
    }

    public static void StoreCertificate(X509Certificate2 cert, StoreName storeName = StoreName.My)
    {
        var store = new X509Store(storeName, StoreLocation.CurrentUser, OpenFlags.ReadWrite);
        store.Add(cert);
    }

    public static X509Certificate2 GetCertificateFromBase64(string base64)
    {
        return new X509Certificate2(Convert.FromBase64String(base64));
    }

    public static bool CertificateExists(string thumbprint, StoreName storeName = StoreName.My)
    {
        if (string.IsNullOrEmpty(thumbprint) ||
            GetCertificate(thumbprint, storeName) is null)
        {
            return false;
        }

        return true;
    }
}
