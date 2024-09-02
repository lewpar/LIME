using LIME.CLI.Commands;

namespace LIME.CLI;

internal class Program
{
    static List<LimeCommand> commands = new List<LimeCommand>();

    static void Main(string[] args)
    {
        RegisterCommands();

        if (args.Length < 1 || 
            args[0].ToLower() == "--help" || 
            args[0].ToLower() == "-h")
        {
            PrintHelp();
            return;
        }

        TryExecuteCommand(args[0].ToLower(), args.Skip(1).ToArray());
    }

    static void RegisterCommands()
    {
        commands.Add(new CreateRootCertificateCmd());
    }

    static void TryExecuteCommand(string command, string[] args)
    {
        LimeCommand? cmd = commands.FirstOrDefault(c => c.Command.ToLower() == command);
        if(cmd is null)
        {
            Console.WriteLine($"Command '{command}' does not exist.");
            return;
        }

        CommandResult paramResult = cmd.TryParseArgs(args);
        if(!paramResult.Result)
        {
            Console.WriteLine($"Failed to execute command '{command}': {paramResult.Message}");
            return;
        }

        CommandResult cmdResult = cmd.TryExecute();
        if(!cmdResult.Result)
        {
            Console.WriteLine($"Failed to execute command '{command}': {cmdResult.Message}");
            return;
        }

        if(!string.IsNullOrWhiteSpace(cmdResult.Message))
        {
            Console.WriteLine(cmdResult.Message);
        }
    }

    static void PrintHelp()
    {
        Console.WriteLine("USAGE:");

        Console.WriteLine($"    lime [{string.Join(" | ", commands.Select(c => c.Command))}] [parameters]");
        Console.WriteLine();
        Console.WriteLine("DOCUMENTATION:");
        foreach(var cmd in commands)
        {
            Console.WriteLine($"    {cmd.Command} - {cmd.Description}");
        }
    }
}
