namespace LIME.Shared.Models;

public class TaskResult
{
    public bool Success { get; }
    public string Message { get; }

    public TaskResult(bool success, string message = "")
    {
        Success = success;
        Message = message;
    }
}
