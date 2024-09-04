using LIME.CLI.Utils;

using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace LIME.CLI.Commands;

internal class RevokeCertificateCmd : LimeCommand
{
    public override string Command => "revoke-cert";
    public override string Description => "Adds a certificate serial number to a intermediate certificates revocation list (CRL).";
    public override string Usage => "revoke-cert";

    public override CommandResult TryExecute()
    {
        try
        {
            X509Certificate2? intCertificate = CertUtils.GetIntermediateCertificate();
            if (intCertificate is null)
            {
                return new CommandResult(false, "No intermediate certificate was found.");
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

            string crlPath = Path.Combine(Program.CrlPath, $"{intCertificate.Issuer.Split('=')[1]}");

            var crlBuilder = CertUtils.GetCrl($"{crlPath}.crl", out BigInteger crlNumber);
            if(crlBuilder is null)
            {
                return new CommandResult(false, "Failed to create Certificate Revocation List builder.");
            }

            crlBuilder.AddEntry(serialBytes, DateTimeOffset.Now);

            var crl = crlBuilder.Build(intCertificate, crlNumber + 1, DateTimeOffset.Now.AddYears(1), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

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
        // Serial number should be even length.
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
