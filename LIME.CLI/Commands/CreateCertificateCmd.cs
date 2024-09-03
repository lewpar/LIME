using LIME.Shared.Crypto;

namespace LIME.CLI.Commands;

internal class CreateCertificateCmd : LimeCommand
{
    public override string Command => "gen-cert";
    public override string Description => "Creates a certiciate signed by an intermediate certificate.";
    public override string Usage => "gen-cert subject=lime.agent int=./intermediate.pfx role=client";

    public CreateCertificateCmd()
    {
        RequiredArgs.Add("subject", "The subject to for the certifiicate.");
        RequiredArgs.Add("int", "The path to the intermediate certificate.");
        RequiredArgs.Add("role", "The role of the certificate. [client | server | webserver]");
    }

    public override CommandResult TryExecute()
    {
        var subject = GetArg("subject");
        var intPath = GetArg("int");
        var role = GetArg("role");

        if(!TryGetAuthRole(role, out X509CertificateAuthRole authRole))
        {
            return new CommandResult(false, $"Invalid auth role '{role}'.");
        }

        try
        {
            var intCertificate = LimeCertificate.ImportCertificate(intPath);
            if (intCertificate is null)
            {
                return new CommandResult(false, $"Intermediate certificate not found at '{intPath}'.");
            }

            var certificate = LimeCertificate.CreateSignedCertificate(intCertificate, subject, authRole, "");
            File.WriteAllBytes($"{subject}.pfx", certificate);

            return new CommandResult(true, $"Created certificate with subject '{subject}'.");
        }
        catch (Exception ex)
        {
            return new CommandResult(false, $"{ex.Message}");
        }
    }

    private bool TryGetAuthRole(string input, out X509CertificateAuthRole role)
    {
        switch (input.ToLower())
        {
            case "client":
                role = X509CertificateAuthRole.Client;
                break;

            case "server":
                role = X509CertificateAuthRole.Server;
                break;

            case "webserver":
                role = X509CertificateAuthRole.WebServer;
                break;

            default:
                role = X509CertificateAuthRole.None;
                return false;
        }

        return true;
    }
}
