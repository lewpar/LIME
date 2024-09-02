namespace LIME.CLI.Commands;

internal class CreateRootCertificateCmd : CliCommand
{
    public string Command => "gen-root";
    public string Description => "Creates a self-signed root certiciate authority.";

    public CommandResult TryExecute(string[] args)
    {
        if(args.Length < 1)
        {
            return new CommandResult(false, "Missing arguments");
        }

        return new CommandResult(true);
    }
}
