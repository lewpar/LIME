using LIME.CLI.Utils;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace LIME.CLI.Commands;

internal class CreateRootCertificateCmd : LimeCommand
{
    public override string Command => "gen-root";
    public override string Description => "Creates a self-signed RSA 2048 root certiciate.";
    public override string Usage => "gen-root";

    public override CommandResult TryExecute()
    {
        var issuer = ConsoleUtils.GetInput("Enter a name for the issuer of the certificate: ");
        var password = ConsoleUtils.GetInput("Enter a password to protect the private key: ");

        var crlUrl = ConsoleUtils.GetInput("Enter the Certificate Revocation List URL: ");

        try
        {
            var certificate = CertUtils.CreateRootCertificate(issuer, crlUrl);

            var privateCert = certificate.Export(X509ContentType.Pkcs12, password);
            var publicCert = certificate.Export(X509ContentType.Cert);

            string rootPath = Program.RootPath;

            if(!Directory.Exists(rootPath))
            {
                Directory.CreateDirectory(rootPath);
            }

            string crlPath = Path.Combine(Program.CrlPath, $"{issuer}.crl");
            var crlBuilder = CertUtils.GetCrl(crlPath, out BigInteger crlNumber);
            if (crlBuilder is null)
            {
                return new CommandResult(false, "Failed to create Certificate Revocation List builder.");
            }

            var crl = crlBuilder.Build(certificate, crlNumber + 1, DateTimeOffset.Now.AddYears(1), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            File.WriteAllBytes(crlPath, crl);

            File.WriteAllBytes(Path.Combine(rootPath, $"{issuer}.private.p12"), privateCert);
            File.WriteAllBytes(Path.Combine(rootPath, $"{issuer}.public.crt"), publicCert);

            return new CommandResult(true, $"Created root certificate with issuer '{issuer}'.");
        }
        catch(Exception ex)
        {
            return new CommandResult(false, $"{ex.Message}");
        }
    }
}
