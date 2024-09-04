using LIME.CLI.Utils;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace LIME.CLI.Commands;

internal class CreateIntermediateCertificateCmd : LimeCommand
{
    public override string Command => "gen-int";
    public override string Description => "Creates an intermediate RSA 2048 certiciate signed by a root certificate.";
    public override string Usage => "gen-int";

    public override CommandResult TryExecute()
    {
        try
        {
            var subject = ConsoleUtils.GetInput("Enter a name for the subject of the certificate: ");
            var password = ConsoleUtils.GetInput("Enter a password to protect the private key: ");
            var crlUrl = ConsoleUtils.GetInput("Enter the Certificate Revocation List URL: ");

            Console.WriteLine();

            var rootCertificate = CertUtils.GetRootCertificate();
            if(rootCertificate is null)
            {
                return new CommandResult(false, "No root certificate was found, use gen-root to create one.");
            }

            var certificate = CertUtils.CreateIntermediateCertificate(rootCertificate, subject, password, crlUrl);

            var privateCert = certificate.Export(X509ContentType.Pkcs12, password);
            var publicCert = certificate.Export(X509ContentType.Cert);

            var privateCertChain = new X509Certificate2Collection()
            {
                new X509Certificate2(rootCertificate.Export(X509ContentType.Cert)),
                certificate
            }.Export(X509ContentType.Pkcs12, password);

            if(privateCertChain is null)
            {
                return new CommandResult(false, "Failed to create certificate chain.");
            }

            string intPath = Program.IntermediatePath;

            if (!Directory.Exists(intPath))
            {
                Directory.CreateDirectory(intPath);
            }

            string crlPath = Path.Combine(Program.CrlPath, $"{subject}.crl");
            var crlBuilder = CertUtils.GetCrl(crlPath, out BigInteger crlNumber);
            if (crlBuilder is null)
            {
                return new CommandResult(false, "Failed to create Certificate Revocation List builder.");
            }

            var crl = crlBuilder.Build(rootCertificate, crlNumber + 1, DateTimeOffset.Now.AddYears(1), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            File.WriteAllBytes(crlPath, crl);

            File.WriteAllBytes(Path.Combine(intPath, $"{subject}.private.p12"), privateCert);
            File.WriteAllBytes(Path.Combine(intPath, $"{subject}.public.crt"), publicCert);

            File.WriteAllBytes(Path.Combine(intPath, $"{subject}.private.chain.p12"), privateCertChain);

            return new CommandResult(true, $"Created intermediate certificate with subject '{subject}'.");
        }
        catch (Exception ex)
        {
            return new CommandResult(false, $"{ex.Message}");
        }
    }
}
