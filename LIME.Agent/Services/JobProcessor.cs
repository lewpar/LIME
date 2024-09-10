using LIME.Agent.Configuration;
using LIME.Agent.Services.Tasks;
using LIME.Shared.Models;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System.Diagnostics;

namespace LIME.Agent.Services;

public class JobProcessor : BackgroundService
{
    private Dictionary<JobType, IJob> jobs;

    private readonly JobQueue queue;
    private readonly LimeAgentConfig config;
    private readonly ILogger<JobProcessor> logger;

    private Stopwatch stopwatch;

    public JobProcessor(JobQueue queue, LimeAgentConfig config, ILogger<JobProcessor> logger)
    {
        jobs = new Dictionary<JobType, IJob>()
        {
            { JobType.Statistics, new StatisticsJob() },
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

            JobContext? task = queue.TryDequeue();
            if (task is null)
            {
                logger.LogCritical("Failed to dequeue a job.");
                continue;
            }

            await ProcessTaskAsync(task);
        }
    }

    private async Task ProcessTaskAsync(JobContext context)
    {
        if (!jobs.ContainsKey(context.Type))
        {
            logger.LogCritical($"No job with the type '{context.Type}' is registered.");
            return;
        }


        logger.LogInformation($"Executing job '{context.Type}'..");

        stopwatch.Restart();
        await jobs[context.Type].ExecuteAsync(context);
        stopwatch.Stop();

        logger.LogInformation($"Job '{context.Type}' finished executing in {stopwatch.ElapsedMilliseconds}ms..");
    }
}
