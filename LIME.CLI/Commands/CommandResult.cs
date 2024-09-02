namespace LIME.CLI.Commands;

internal class CommandResult
{
    public bool Result { get; }
    public string Message { get; }

    public CommandResult(bool result, string message = "")
    {
        Result = result;
        Message = message;
    }
}
