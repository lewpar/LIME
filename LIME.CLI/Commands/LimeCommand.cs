namespace LIME.CLI.Commands;

internal abstract class LimeCommand
{
    public virtual string Command { get; } = "";
    public virtual string Description { get; } = "";
    public virtual string Usage { get; } = "";

    public virtual CommandResult TryExecute() { return new CommandResult(false); }

    public string GetInput(string prompt)
    {
        Console.Write(prompt);

        string? input = null;
        while(string.IsNullOrWhiteSpace(input))
        {
            input = Console.ReadLine();
        }

        return input;
    }
}
