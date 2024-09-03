using LIME.CLI.Utils;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace LIME.CLI.Commands;

internal class RevokeCertificateCmd : LimeCommand
{
    public override string Command => "revoke-cert";
    public override string Description => "Adds a certificate serial number to a root certificates revocation list (CRL).";
    public override string Usage => "revoke-cert";

    public override CommandResult TryExecute()
    {
        try
        {
            X509Certificate2? rootCertificate = CertUtils.GetRootCertificate();
            if (rootCertificate is null)
            {
                return new CommandResult(false, "No root certificate was found.");
            }

            var serialNumber = ConsoleUtils.GetInput("Enter serial number of certificate to revoke: ");
            if(!TryParseSerialNumber(serialNumber, out byte[] serialBytes))
            {
                return new CommandResult(false, "Invalid serial number.");
            }

            if(!Directory.Exists(Program.CrlPath))
            {
                Directory.CreateDirectory(Program.CrlPath);
            }

            string crlPath = Path.Combine(Program.CrlPath, $"{rootCertificate.Issuer.Split('=')[1]}.crl");

            byte[]? crl = LoadCrl(crlPath);

            CertificateRevocationListBuilder crlBuilder;
            BigInteger crlNumber;

            if (crl is null)
            {
                crlBuilder = new CertificateRevocationListBuilder();
                crlNumber = 1;
            }
            else
            {
                crlBuilder = CertificateRevocationListBuilder.Load(crl, out crlNumber);
                crlNumber = crlNumber + 1;
            }

            crlBuilder.AddEntry(serialBytes, DateTimeOffset.Now);

            crl = crlBuilder.Build(rootCertificate, crlNumber, DateTimeOffset.Now.AddYears(1), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            File.WriteAllBytes(crlPath, crl);

            return new CommandResult(true);
        }
        catch (Exception ex)
        {
            return new CommandResult(false, $"{ex.Message}");
        }
    }

    private bool TryParseSerialNumber(string serialNumber, out byte[] serialBytes)
    {
        // Serial number should even length.
        if(serialNumber.Length % 2 != 0)
        {
            serialBytes = new byte[0];
            return false;
        }

        byte[] bytes = new byte[serialNumber.Length / 2];
        for (int i = 0; i < serialNumber.Length; i += 2)
        {
            // Convert each pair of hex characters to a byte
            bytes[i / 2] = Convert.ToByte(serialNumber.Substring(i, 2), 16);
        }

        serialBytes = bytes;

        return true;
    }

    private byte[]? LoadCrl(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        return File.ReadAllBytes(path);
    }
}
