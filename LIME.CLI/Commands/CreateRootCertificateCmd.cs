using LIME.Shared.Crypto;

namespace LIME.CLI.Commands;

internal class CreateRootCertificateCmd : LimeCommand
{
    public override string Command => "gen-root";
    public override string Description => "Creates a self-signed root certiciate.";

    public CreateRootCertificateCmd()
    {
        RequiredArgs.Add("password");
        RequiredArgs.Add("issuer");
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
