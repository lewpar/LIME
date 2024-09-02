namespace LIME.CLI.Commands;

internal interface CliCommand
{
    string Command { get; }
    string Description { get; }

    CommandResult TryExecute(string[] args);
}
