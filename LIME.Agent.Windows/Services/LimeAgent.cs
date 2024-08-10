using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;

namespace LIME.Agent.Windows.Services;

internal class LimeAgent : IHostedService
{
    private readonly ILogger<LimeAgent> logger;
    private readonly IConfiguration config;

    private IPEndPoint? mediatorEndpoint;

    public LimeAgent(ILogger<LimeAgent> logger, IConfiguration config)
    {
        this.logger = logger;
        this.config = config;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Lime Agent Started.");

        ParseConfig();
    }

    private void ParseConfig()
    {
        var addressEntry = config["MediatorAddress"];
        if (string.IsNullOrEmpty(addressEntry))
        {
            logger.LogCritical("MediatorAddress not found in appsettings.json");
            return;
        }

        if (!IPAddress.TryParse(addressEntry, out IPAddress? address))
        {
            logger.LogCritical("MediatorAddress is invalid in appsettings.json");
            return;
        }

        if(address is null)
        {
            logger.LogCritical("Failed to parse MediatorAddress in appsettings.json");
            return;
        }

        var portEntry = config["MediatorPort"];
        if(string.IsNullOrEmpty(portEntry))
        {
            logger.LogCritical("MediatorPort not found in appsettings.json");
            return;
        }

        if (!int.TryParse(portEntry, out int port))
        {
            logger.LogCritical("MediatorPort is invalid in appsettings.json");
            return;
        }

        mediatorEndpoint = new IPEndPoint(address, port);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Lime Agent Stopped.");
    }
}
