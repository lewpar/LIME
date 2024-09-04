using System.Numerics;

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace LIME.CLI.Utils;

internal class CertUtils
{
    public static X509Certificate2? GetRootCertificate()
    {
        if (!Directory.Exists(Program.RootPath))
        {
            return null;
        }

        string[] rootFiles = Directory.GetFiles(Program.RootPath, "*.private.p12");
        if (rootFiles.Length < 1)
        {
            return null;
        }

        string rootPath;
        if (rootFiles.Length > 1)
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

                if (int.TryParse(ConsoleUtils.GetInput("> "), out int input))
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

        var password = ConsoleUtils.GetInput("Enter password for root certificate: ");

        return new X509Certificate2(rootPath, password);
    }

    public static X509Certificate2? GetIntermediateCertificate()
    {
        if (!Directory.Exists(Program.IntermediatePath))
        {
            return null;
        }

        string[] intFiles = Directory.GetFiles(Program.IntermediatePath, "*.private.p12");
        if (intFiles.Length < 1)
        {
            return null;
        }

        string intPath;
        if (intFiles.Length > 1)
        {
            int? selectedIndex = null;

            while (selectedIndex is null)
            {
                Console.WriteLine("Multiple intermediate certificates detected, please select one.");

                for (int i = 0; i < intFiles.Length; i++)
                {
                    string intFile = intFiles[i];
                    Console.WriteLine($"{i}) {Path.GetFileName(intFile)}");
                }

                if (int.TryParse(ConsoleUtils.GetInput("> "), out int input))
                {
                    if (input > intFiles.Length)
                    {
                        continue;
                    }

                    selectedIndex = input;
                }
            }

            intPath = intFiles[selectedIndex.Value];
        }
        else
        {
            intPath = intFiles[0];
        }

        var password = ConsoleUtils.GetInput("Enter password for intermediate certificate: ");

        return new X509Certificate2(intPath, password);
    }

    public static X509Certificate2 CreateRootCertificate(string issuer)
    {
        using var rsa = RSA.Create(2048);

        var subject = new X500DistinguishedName($"CN={issuer}");

        var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign |
                                                                    X509KeyUsageFlags.DigitalSignature |
                                                                    X509KeyUsageFlags.NonRepudiation |
                                                                    X509KeyUsageFlags.CrlSign, true));

        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection
        {
            new Oid("1.3.6.1.5.5.7.3.2"), // Client Authentication
            new Oid("1.3.6.1.5.5.7.3.1") // Server Authentication
        }, false));

        return request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));
    }

    public static X509Certificate2 CreateIntermediateCertificate(X509Certificate2 rootCertificate, string subject, string password, string crlUrl)
    {
        using var rsa = RSA.Create(2048);

        var subjectName = new X500DistinguishedName($"CN={subject}");

        var request = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign |
                                                                    X509KeyUsageFlags.DigitalSignature |
                                                                    X509KeyUsageFlags.NonRepudiation |
                                                                    X509KeyUsageFlags.CrlSign, true));

        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection
        {
            new Oid("1.3.6.1.5.5.7.3.2"), // Client Authentication
            new Oid("1.3.6.1.5.5.7.3.1") // Server Authentication
        }, false));

        // Certificate Revocation List
        request.CertificateExtensions.Add(CertificateRevocationListBuilder.BuildCrlDistributionPointExtension(new[] { crlUrl }));

        var certificate = request.Create(rootCertificate, DateTimeOffset.Now, rootCertificate.NotAfter, Guid.NewGuid().ToByteArray());

        return certificate.CopyWithPrivateKey(rsa);
    }

    public static CertificateRevocationListBuilder GetCrl(string path, out BigInteger crlNumber)
    {
        if (!File.Exists(path))
        {
            crlNumber = BigInteger.Zero;
            return new CertificateRevocationListBuilder();
        }

        var crl = File.ReadAllBytes(path);

        return CertificateRevocationListBuilder.Load(crl, out crlNumber);;
    }
}
