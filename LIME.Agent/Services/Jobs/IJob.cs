namespace LIME.Agent.Services.Tasks;

public interface IJob
{
    Task ExecuteAsync(JobContext context);
}
