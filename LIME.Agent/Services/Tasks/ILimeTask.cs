namespace LIME.Agent.Services.Tasks;

public interface ILimeTask
{
    Task ExecuteAsync(TaskContext context);
}
