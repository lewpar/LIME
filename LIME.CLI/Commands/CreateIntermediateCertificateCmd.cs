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
            var subject = GetInput("Enter a name for the subject of the certificate: ");
            var password = GetInput("Enter a password to protect the private key: ");

            Console.WriteLine();

            var rootCertificate = GetRootCertificate();
            if(rootCertificate is null)
            {
                return new CommandResult(false, "No root certificate was found, use gen-root to create one.");
            }

            var certificate = CreateIntermediateCertificate(rootCertificate, subject, password);

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

            File.WriteAllBytes(Path.Combine(intPath, $"{subject}.private.p12"), privateCert);
            File.WriteAllBytes(Path.Combine(intPath, $"{subject}.public.crt"), publicCert);

            File.WriteAllBytes(Path.Combine(intPath, $"{subject}.private.chain.p12"), privateCertChain);

            return new CommandResult(true, $"Created intermediate certificate with subject '{subject}'.");
        }
        catch (Exception ex)
        {
            return new CommandResult(false, $"{ex.Message}");
        }

        return new CommandResult(true);
    }

    private X509Certificate2? GetRootCertificate()
    {
        if(!Directory.Exists(Program.RootPath))
        {
            return null;
        }

        string[] rootFiles = Directory.GetFiles(Program.RootPath, "*.private.p12");
        if(rootFiles.Length < 1)
        {
            return null;
        }

        string rootPath;
        if(rootFiles.Length > 1)
        {
            int? selectedIndex = null;

            while (selectedIndex is null)
            {
                Console.WriteLine("Multiple root certificates detected, please select one.");

                for (int i = 0; i < rootFiles.Length; i++)
                {
                    string rootFile = rootFiles[i];
                    Console.WriteLine($"{i}) {Path.GetFileName(rootFile)}");
                }

                if (int.TryParse(GetInput("> "), out int input))
                {
                    if (input > rootFiles.Length)
                    {
                        continue;
                    }

                    selectedIndex = input;
                }
            }

            rootPath = rootFiles[selectedIndex.Value];
        }
        else
        {
            rootPath = rootFiles[0];
        }

        var password = GetInput("Enter password for root certificate: ");

        return new X509Certificate2(rootPath, password);
    }

    private X509Certificate2 CreateIntermediateCertificate(X509Certificate2 rootCertificate, string subject, string password)
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

        return certificate.CopyWithPrivateKey(rsa);
    }
}
