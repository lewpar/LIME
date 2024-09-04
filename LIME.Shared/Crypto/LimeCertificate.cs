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

    public static X509Certificate2 CreateServerCertificate(X509Certificate2 issuer, string subject, string crlUrl, string dns = "")
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

        // Certificate Revocation List
        request.CertificateExtensions.Add(CertificateRevocationListBuilder.BuildCrlDistributionPointExtension(new[] { crlUrl }));

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
    /// Takes a certificate chain and stores each of the certificates in their respective store.
    /// </summary>
    /// <param name="chain">The chain of certificates to store.</param>
    public static void StoreCertificateChain(X509Certificate2Collection chain, bool replaceExisting = false)
    {
        foreach (var certificate in chain)
        {
            if (IsRootCertificate(certificate))
            {
                StoreCertificate(certificate, StoreName.Root, replaceExisting);
            }
            else if (IsIntermediateCertificate(certificate))
            {
                StoreCertificate(certificate, StoreName.CertificateAuthority, replaceExisting);
            }
            else
            {
                StoreCertificate(certificate, StoreName.My, replaceExisting);
            }
        }
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

    /// <summary>
    /// Imports an X509 certificate chain as a collection.
    /// </summary>
    /// <param name="path">The path to the certificate.</param>
    /// <returns>The certificate chain.</returns>
    public static X509Certificate2Collection ImportChain(string path)
    {
        var chain = new X509Certificate2Collection();
        chain.Import(path);

        return chain;
    }

    /// <summary>
    /// Checks to see if a certificate is a root certificate
    /// by checking if it is self-signed.
    /// </summary>
    /// <param name="cert">The certificate to check.</param>
    /// <returns></returns>
    public static bool IsRootCertificate(X509Certificate2 cert)
    {
        return cert.Issuer == cert.Subject;
    }

    /// <summary>
    /// Checks to see if a certificate is a intermediate certificate 
    /// by checking if it has the CertificateAuthority basic constraint and is not self-signed.
    /// </summary>
    /// <param name="cert">The certificate to check.</param>
    /// <returns></returns>
    public static bool IsIntermediateCertificate(X509Certificate2 cert)
    {
        foreach (var extension in cert.Extensions)
        {
            if (extension is X509BasicConstraintsExtension basicConstraints)
            {
                return basicConstraints.CertificateAuthority && cert.Subject != cert.Issuer;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks to see if the certifice collection is a two-tier chain containing a root, intermediate and end-entity certificates.
    /// </summary>
    /// <param name="chain">The certificate chain to check.</param>
    /// <returns></returns>
    public static bool IsTieredChain(X509Certificate2Collection chain)
    {
        if (chain.Count < 3)
        {
            return false;
        }

        bool rootFound = false;
        bool intFound = false;
        bool certFound = false;

        foreach (var certificate in chain)
        {
            if (IsRootCertificate(certificate))
            {
                rootFound = true;
            }
            else if (IsIntermediateCertificate(certificate))
            {
                intFound = true;
            }
            else
            {
                certFound = true;
            }
        }

        return rootFound && intFound && certFound;
    }
}
