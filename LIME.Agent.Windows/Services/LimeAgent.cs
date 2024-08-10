using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LIME.Agent.Windows.Services;

internal class LimeAgent : IHostedService
{
    private readonly ILogger<LimeAgent> logger;

    public LimeAgent(ILogger<LimeAgent> logger)
    {
        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Lime Agent Started.");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Lime Agent Stopped.");
    }
}
