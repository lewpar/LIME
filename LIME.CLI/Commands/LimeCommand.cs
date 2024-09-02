namespace LIME.CLI.Commands;

internal abstract class LimeCommand
{
    public virtual string Command { get; } = "";
    public virtual string Description { get; } = "";

    public virtual List<string> RequiredArgs { get; } = new List<string>();
    public Dictionary<string, string> Args { get; } = new Dictionary<string, string>();

    public virtual CommandResult TryExecute() { return new CommandResult(false); }

    public CommandResult TryParseArgs(string[] args)
    {
        if(args.Length < RequiredArgs.Count)
        {
            var missingArgs = RequiredArgs.Except(args, StringComparer.OrdinalIgnoreCase);

            return new CommandResult(false, $"Missing required args '{string.Join(", ", missingArgs)}'.");
        }

        bool missingValues = false;
        List<string> missingKeys = new List<string>();

        foreach (var arg in args)
        {
            var parts = arg.Split('=');
            if(parts.Length != 2 || 
                string.IsNullOrWhiteSpace(parts[1]))
            {
                missingValues = true;
                missingKeys.Add(parts[0]);
                continue;
            }

            Args.Add(parts[0].ToLower(), parts[1].ToLower());
        }

        if(missingValues)
        {
            return new CommandResult(false, $"Missing value for arg(s) '{string.Join(", ", missingKeys)}'.");
        }

        return new CommandResult(true);
    }

    public string GetArg(string key)
    {
        return Args[key] ?? "";
    }
}
