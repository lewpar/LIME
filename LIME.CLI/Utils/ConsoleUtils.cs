namespace LIME.CLI.Utils;

internal class ConsoleUtils
{
    public static string GetInput(string prompt)
    {
        Console.Write(prompt);

        string? input = null;
        while (string.IsNullOrWhiteSpace(input))
        {
            input = Console.ReadLine();
        }

        return input;
    }
}
