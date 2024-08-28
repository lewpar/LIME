namespace LIME.Shared.Extensions;

public static class ConsoleHelper
{
    public static bool? RequestYesNo(string message, string yes, string no, bool yesDefault = true)
    {
        Console.Write($"{message} {(yesDefault ? $"[{yes}]" : yes)}/{(yesDefault ? no : $"[{no}]")}: ");

        var response = Console.ReadLine();
        if(response is null)
        {
            return null;
        }

        if(string.IsNullOrWhiteSpace(response))
        {
            return yesDefault;
        }

        return response.ToLower() switch
        {
            var r when r == yes.ToLower() => true,
            var r when r == no.ToLower() => false,
            _ => yesDefault
        };
    }

    public static void RequestEnter()
    {
        Console.Write("Press ENTER to continue.");
        Console.ReadLine();
    }

    public static int? RequestNumber(string message)
    {
        Console.Write(message);
        var response = Console.ReadLine();

        if(!int.TryParse(response, out var number))
        {
            return null;
        }

        return number;
    }
}
