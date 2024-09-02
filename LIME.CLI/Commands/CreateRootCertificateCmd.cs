namespace LIME.CLI.Commands;

internal class CreateRootCertificateCmd : LimeCommand
{
    public override string Command => "gen-root";
    public override string Description => "Creates a self-signed root certiciate authority.";

    public CreateRootCertificateCmd()
    {
        RequiredArgs.Add("password");
        RequiredArgs.Add("issuer");
    }

    public override CommandResult TryExecute()
    {
        var password = GetArg("password");
        var issuer = GetArg("issuer");

        return new CommandResult(true, $"Creating password protected root certificate with issuer '{issuer}'.");
    }
}
