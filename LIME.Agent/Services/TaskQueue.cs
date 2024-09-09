using System.Collections.Concurrent;

namespace LIME.Agent.Services;

public class TaskQueue
{
    private ConcurrentQueue<TaskContext> tasks;

    public TaskQueue()
    {
        tasks = new ConcurrentQueue<TaskContext>();
    }

    public void Enqueue(TaskContext task)
    {
        tasks.Enqueue(task);
    }

    public TaskContext? TryDequeue()
    {
        tasks.TryDequeue(out TaskContext? task);

        return task;
    }

    public bool HasTasks()
    {
        return tasks.Count > 0;
    }
}
