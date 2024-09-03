using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace LIME.CLI.Commands;

internal class CreateRootCertificateCmd : LimeCommand
{
    public override string Command => "gen-root";
    public override string Description => "Creates a self-signed RSA 2048 root certiciate.";
    public override string Usage => "gen-root";

    public override CommandResult TryExecute()
    {
        var issuer = GetInput("Enter a name for the issuer of the certificate: ");
        var password = GetInput("Enter a password to protect the private key: ");

        var crlUrl = GetInput("Enter the Certificate Revocation List URL: ");

        try
        {
            var certificate = CreateRootCertificate(issuer, crlUrl);

            var privateCert = certificate.Export(X509ContentType.Pkcs12, password);
            var publicCert = certificate.Export(X509ContentType.Cert);

            string rootPath = Program.RootPath;

            if(!Directory.Exists(rootPath))
            {
                Directory.CreateDirectory(rootPath);
            }

            File.WriteAllBytes(Path.Combine(rootPath, $"{issuer}.private.p12"), privateCert);
            File.WriteAllBytes(Path.Combine(rootPath, $"{issuer}.public.crt"), publicCert);

            return new CommandResult(true, $"Created root certificate with issuer '{issuer}'.");
        }
        catch(Exception ex)
        {
            return new CommandResult(false, $"{ex.Message}");
        }
    }

    private X509Certificate2 CreateRootCertificate(string issuer, string crlUrl)
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

        // Certificate Revocation List
        request.CertificateExtensions.Add(CertificateRevocationListBuilder.BuildCrlDistributionPointExtension(new[] { crlUrl }));

        return request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));
    }
}
