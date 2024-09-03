using LIME.Shared.Crypto;

namespace LIME.CLI.Commands;

internal class CreateIntermediateCertificateCmd : LimeCommand
{
    public override string Command => "gen-int";
    public override string Description => "Creates an intermediate RSA 2048 certiciate signed by a root certificate.";
    public override string Usage => "gen-int rootpassword=hunter1 subject=lime.intermediate root=./root.pfx";

    public CreateIntermediateCertificateCmd()
    {
        RequiredArgs.Add("subject", "The subject to for the certifiicate.");
        RequiredArgs.Add("root", "The path to the root certificate.");
        RequiredArgs.Add("rootpassword", "The password required to access the root certificate.");
        RequiredArgs.Add("password", "The password to protect the certificate.");
    }

    public override CommandResult TryExecute()
    {
        var rootPassword = GetArg("rootpassword");
        var subject = GetArg("subject");
        var rootPath = GetArg("root");
        var password = GetArg("password");

        try
        {
            var rootCertificate = LimeCertificate.ImportCertificate(rootPath, rootPassword);
            if(rootCertificate is null)
            {
                return new CommandResult(false, $"Root certificate not found at '{rootPath}'.");
            }

            var certificate = LimeCertificate.CreateIntermediateCertificate(rootCertificate, subject, password);
            File.WriteAllBytes($"{subject}.pfx", certificate);

            return new CommandResult(true, $"Created intermediate certificate with subject '{subject}'.");
        }
        catch (Exception ex)
        {
            return new CommandResult(false, $"{ex.Message}");
        }
    }
}
