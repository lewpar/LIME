using LIME.CLI.Utils;

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
