using LIME.Shared.Crypto;

namespace LIME.CLI.Commands;

internal class CreateRootCertificateCmd : LimeCommand
{
    public override string Command => "gen-root";
    public override string Description => "Creates a self-signed RSA 2048 root certiciate.";
    public override string Usage => "gen-root password=hunter1 issuer=lime";

    public CreateRootCertificateCmd()
    {
        RequiredArgs.Add("password", "The password to protect the certificate.");
        RequiredArgs.Add("issuer", "The issuer to for the self-signed certifiicate.");
    }

    public override CommandResult TryExecute()
    {
        var password = GetArg("password");
        var issuer = GetArg("issuer");

        try
        {
            var certificate = LimeCertificate.CreateRootCertificate(issuer, password);
            File.WriteAllBytes($"{issuer}.pfx", certificate);

            return new CommandResult(true, $"Created password protected root certificate with issuer '{issuer}'.");
        }
        catch(Exception ex)
        {
            return new CommandResult(false, $"{ex.Message}");
        }
    }
}
