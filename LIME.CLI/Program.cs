using LIME.CLI.Commands;

namespace LIME.CLI;

internal class Program
{
    static List<CliCommand> commands = new List<CliCommand>();

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

        TryExecuteCommand(args[0], args.Skip(1).ToArray());
    }

    static void RegisterCommands()
    {
        commands.Add(new CreateRootCertificateCmd());
    }

    static void TryExecuteCommand(string command, string[] args)
    {
        CliCommand? cmd = commands.FirstOrDefault(c => c.Command == command);
        if(cmd is null)
        {
            Console.WriteLine($"Command '{command}' does not exist.");
            return;
        }

        CommandResult result = cmd.TryExecute(args);

        if(!result.Result)
        {
            Console.WriteLine($"Failed to execute command '{command}': ");
            return;
        }
    }

    static void PrintHelp()
    {
        var tab = "    ";

        Console.WriteLine("USAGE:");

        Console.WriteLine($"{tab}lime [{string.Join(" | ", commands.Select(c => c.Command))}]");
        Console.WriteLine();
        Console.WriteLine("DOCUMENTATION:");
        foreach(var cmd in commands)
        {
            Console.WriteLine($"{tab}{cmd.Command} - {cmd.Description}");
        }
    }
}
