using LIME.Agent.Configuration;
using LIME.Agent.Services.Tasks;
using LIME.Shared.Models;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace LIME.Agent.Services;

public class TaskProcessor : BackgroundService
{
    private Dictionary<LimeTaskType, ILimeTask> tasks;

    private readonly TaskQueue queue;
    private readonly LimeAgentConfig config;
    private readonly ILogger<TaskProcessor> logger;

    private Stopwatch stopwatch;

    public TaskProcessor(TaskQueue queue, LimeAgentConfig config, ILogger<TaskProcessor> logger)
    {
        tasks = new Dictionary<LimeTaskType, ILimeTask>()
        {
            { LimeTaskType.Statistics, new StatisticsTask() },
        };

        this.queue = queue;
        this.config = config;
        this.logger = logger;

        this.stopwatch = new Stopwatch();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(config.TaskFrequency * 1000);

            if(!queue.HasTasks())
            {
                continue;
            }

            TaskContext? task = queue.TryDequeue();
            if (task is null)
            {
                logger.LogCritical("Failed to dequeue a task.");
                continue;
            }

            await ProcessTaskAsync(task);
        }
    }

    private async Task ProcessTaskAsync(TaskContext context)
    {
        LimeTask task = context.Task;

        if (!tasks.ContainsKey(task.Type))
        {
            logger.LogCritical($"No task with the type '{task.Type}' is registered.");
            return;
        }


        logger.LogInformation($"Executing task '{task.Type}'..");

        stopwatch.Restart();
        await tasks[task.Type].ExecuteAsync(context);
        stopwatch.Stop();

        logger.LogInformation($"Task '{task.Type}' finished executing in {stopwatch.ElapsedMilliseconds}ms..");
    }
}
