using System.Collections.Concurrent;

namespace LIME.Agent.Services;

public class JobQueue
{
    private ConcurrentQueue<JobContext> tasks;

    public JobQueue()
    {
        tasks = new ConcurrentQueue<JobContext>();
    }

    public void Enqueue(JobContext task)
    {
        tasks.Enqueue(task);
    }

    public JobContext? TryDequeue()
    {
        tasks.TryDequeue(out JobContext? task);

        return task;
    }

    public bool HasTasks()
    {
        return tasks.Count > 0;
    }
}
