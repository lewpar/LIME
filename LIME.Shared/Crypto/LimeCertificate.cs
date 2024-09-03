using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace LIME.Shared.Crypto;

public class LimeCertificate
{
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

    /// <summary>
    /// Creates a self-signed root certificate.
    /// </summary>
    /// <param name="issuer">The issuer for the new certificate.</param>
    /// <returns>The self-signed root certificate.</returns>
    public static X509Certificate2 CreateRootCertificate(string issuer, string password = "", bool exportable = false)
    {
        return new X509Certificate2(CreateRootCertificate(issuer, password), password, exportable ? X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet : X509KeyStorageFlags.DefaultKeySet);
    }

    /// <summary>
    /// Creates a self-signed root certificate.
    /// </summary>
    /// <param name="issuer">The issuer for the new certificate.</param>
    /// <returns>The self-signed root certificate.</returns>
    public static byte[] CreateRootCertificate(string issuer, string password = "")
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

        return certificate.Export(X509ContentType.Pfx, password);
    }

    /// <summary>
    /// Creates an intermediate certificate signed by the root certificate.
    /// </summary>
    /// <param name="rootCertificate">The root certificate.</param>
    /// <param name="subject">The subject of the new intermediate certificate.</param>
    /// <returns>The signed intermediate certificate.</returns>
    public static X509Certificate2 CreateIntermediateCertificate(X509Certificate2 rootCertificate, string subject, string password = "", bool exportable = false)
    {
        return new X509Certificate2(CreateIntermediateCertificate(rootCertificate, subject, password), password, exportable ? X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet : X509KeyStorageFlags.DefaultKeySet);
    }

    /// <summary>
    /// Creates an intermediate certificate signed by the root certificate.
    /// </summary>
    /// <param name="rootCertificate">The root certificate.</param>
    /// <param name="subject">The subject of the new intermediate certificate.</param>
    /// <returns>The signed intermediate certificate.</returns>
    public static byte[] CreateIntermediateCertificate(X509Certificate2 rootCertificate, string subject, string password = "")
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

        return certificatePrivate.Export(X509ContentType.Pkcs12, password);
    }

    /// <summary>
    /// Creates a signed certificate from an intermediate certificate.
    /// </summary>
    /// <param name="issuer">The intermediate certificate.</param>
    /// <param name="subject">The subject of the new certificate.</param>
    /// <param name="role">The authentication role of the certificate.</param>
    /// <returns>The signed certificate.</returns>
    public static X509Certificate2 CreateSignedCertificate(X509Certificate2 issuer, string subject, X509CertificateAuthRole role, string password = "", bool exportable = false)
    {
        return new X509Certificate2(CreateSignedCertificate(issuer, subject, role, password), password, exportable ? X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet : X509KeyStorageFlags.DefaultKeySet);
    }

    /// <summary>
    /// Creates a signed certificate from an intermediate certificate.
    /// </summary>
    /// <param name="issuer">The intermediate certificate.</param>
    /// <param name="subject">The subject of the new certificate.</param>
    /// <param name="role">The authentication role of the certificate.</param>
    /// <returns>The signed certificate.</returns>
    public static byte[] CreateSignedCertificate(X509Certificate2 issuer, string subject, X509CertificateAuthRole role, string password = "")
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

        if (role == X509CertificateAuthRole.WebServer)
        {
            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddDnsName(subject);

            request.CertificateExtensions.Add(sanBuilder.Build());
        }

        var certificate = request.Create(issuer, DateTimeOffset.Now, issuer.NotAfter, Guid.NewGuid().ToByteArray());

        var certificatePrivate = certificate.CopyWithPrivateKey(rsa);

        return certificatePrivate.Export(X509ContentType.Pkcs12, password);
    }

    /// <summary>
    /// Forms a bundled certificate for exporting a signed certificate. Root and intermediate are distributed with the public key only.
    /// </summary>
    /// <param name="root">The root certificate.</param>
    /// <param name="intermediate">The intermediate certificated signed by the root.</param>
    /// <param name="certificate">The certificate signed by the intermediate.</param>
    /// <returns>The bundled certificate chain in PFX format.</returns>
    public static byte[]? CreateBundledCertificate(X509Certificate2 root, X509Certificate2 intermediate, X509Certificate2 certificate)
    {
        var collection = new X509Certificate2Collection()
        {
            new X509Certificate2(root.Export(X509ContentType.Cert)),
            new X509Certificate2(intermediate.Export(X509ContentType.Cert)),
            certificate
        };

        var cert = collection.Export(X509ContentType.Pfx);
        if(cert is null)
        {
            return null;
        }

        return cert;
    }

    /// <summary>
    /// Imports an X509 certificate chain as a bundled collection.
    /// </summary>
    /// <param name="path">The path to the certificate.</param>
    /// <returns>The certificate chain.</returns>
    public static X509Certificate2Collection ImportBundledCertificate(string path)
    {
        var chain = new X509Certificate2Collection();
        chain.Import(path);

        return chain;
    }

    /// <summary>
    /// Checks to see if the certifice collection is a two-tier chain containing a root, intermediate and end-entity certificates.
    /// </summary>
    /// <param name="chain">The certificate chain to check.</param>
    /// <returns></returns>
    public static bool IsTieredChain(X509Certificate2Collection chain)
    {
        if(chain.Count < 3)
        {
            return false;
        }

        bool rootFound = false; 
        bool intFound = false;
        bool certFound = false;

        foreach(var certificate in chain)
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

    /// <summary>
    /// Takes a bundled certificate chain and stores each of the certificates in their respective store.
    /// </summary>
    /// <param name="bundledCertificate">The bundle of certificate to store.</param>
    public static void StoreBundledCertificate(X509Certificate2Collection bundledCertificate, bool replaceExisting = false)
    {
        foreach (var certificate in bundledCertificate)
        {
            if(IsRootCertificate(certificate))
            {
                StoreCertificate(certificate, StoreName.Root, replaceExisting);
            }
            else if(IsIntermediateCertificate(certificate))
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
    /// Stores a certificate in the X509Store.
    /// </summary>
    /// <param name="cert">The certificate to store.</param>
    /// <param name="storeName">The location where the certificate will be stored.</param>
    public static void StoreCertificate(X509Certificate2 cert, StoreName storeName = StoreName.My, bool replaceExisting = false)
    {
        var store = new X509Store(storeName, StoreLocation.CurrentUser, OpenFlags.ReadWrite);

        if(replaceExisting)
        {
            var existingCertificate = store.Certificates.FirstOrDefault(c => c.Issuer == cert.Issuer && 
                                                                             c.Subject == c.Subject);
            if(existingCertificate is not null)
            {
                store.Remove(existingCertificate);
            }
        }

        store.Add(cert);
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

    public static X509Certificate2? ImportCertificate(string path, string password = "")
    {
        if(!File.Exists(path))
        {
            return null;
        }

        return new X509Certificate2(path, password);
    }
}
