using System.Data;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace LIME.Shared.Crypto;

public class LimeCertificate
{
    public static X509Certificate2 CreateClientCertificate(X509Certificate2 issuer, string subject)
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
            new Oid("1.3.6.1.5.5.7.3.2") // Client Authentication
        }, false));

        var certificate = request.Create(issuer, DateTimeOffset.Now, issuer.NotAfter, Guid.NewGuid().ToByteArray());

        return certificate.CopyWithPrivateKey(rsa);
    }

    public static X509Certificate2 CreateServerCertificate(X509Certificate2 issuer, string subject, string dns = "")
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
            new Oid("1.3.6.1.5.5.7.3.1") // Server Authentication
        }, false));

        if(!string.IsNullOrWhiteSpace(dns))
        {
            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddDnsName(dns);

            request.CertificateExtensions.Add(sanBuilder.Build());
        }

        var certificate = request.Create(issuer, DateTimeOffset.Now, issuer.NotAfter, Guid.NewGuid().ToByteArray());

        return certificate.CopyWithPrivateKey(rsa);
    }

    /// <summary>
    /// Gets an X509 certificate from the X509Store.
    /// </summary>
    /// <param name="thumbprint">The thumbprint of the certificate to get.</param>
    /// <param name="storeName">The store to check.</param>
    /// <returns>The X509 certificate.</returns>
    public static X509Certificate2? GetCertificate(string thumbprint, StoreName storeName = StoreName.My)
    {
        var store = new X509Store(storeName, StoreLocation.CurrentUser, OpenFlags.ReadOnly);
        var certificates = store.Certificates;

        if (certificates is null)
        {
            return null;
        }

        foreach (var certificate in certificates)
        {
            // Do not get expired certificate
            if (DateTime.Now > certificate.NotAfter)
            {
                continue;
            }

            if (certificate.Thumbprint.ToLower() == thumbprint.ToLower())
            {
                return certificate;
            }
        }

        return null;
    }

    /// <summary>
    /// Stores a certificate in the X509Store.
    /// </summary>
    /// <param name="cert">The certificate to store.</param>
    /// <param name="storeName">The location where the certificate will be stored.</param>
    public static void StoreCertificate(X509Certificate2 cert, StoreName storeName = StoreName.My, bool replaceExisting = false)
    {
        var store = new X509Store(storeName, StoreLocation.CurrentUser, OpenFlags.ReadWrite);

        if (replaceExisting)
        {
            var existingCertificate = store.Certificates.FirstOrDefault(c => c.Issuer == cert.Issuer &&
                                                                             c.Subject == cert.Subject);
            if (existingCertificate is not null)
            {
                store.Remove(existingCertificate);
            }
        }

        store.Add(new X509Certificate2(cert.Export(X509ContentType.Pfx), "", X509KeyStorageFlags.PersistKeySet));
    }

    /// <summary>
    /// Checks to see if a certificate exists in the X509Store.
    /// </summary>
    /// <param name="thumbprint">The thumb/fingerprint of the certificate to check.</param>
    /// <param name="storeName">The certificate store to check.</param>
    /// <returns></returns>
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
