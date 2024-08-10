using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System.Net;
using System.Net.Sockets;

namespace LIME.Agent.Windows.Services;

internal class LimeAgent : IHostedService
{
    private readonly ILogger<LimeAgent> logger;
    private readonly IConfiguration config;

    private IPEndPoint mediatorEndpoint;
    private TcpClient client;

    public LimeAgent(ILogger<LimeAgent> logger, IConfiguration config)
    {
        this.logger = logger;
        this.config = config;

        client = new TcpClient();
        mediatorEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 55123);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Lime Agent Started.");

        if(!TryParseConfig())
        {
            return;
        }

        if(!await TryConnectToMediatorAsync())
        {
            return;
        }
    }

    private bool TryParseConfig()
    {
        var addressEntry = config["MediatorAddress"];
        if (string.IsNullOrEmpty(addressEntry))
        {
            logger.LogCritical("MediatorAddress not found in appsettings.json");
            return false;
        }

        if (!IPAddress.TryParse(addressEntry, out IPAddress? address))
        {
            logger.LogCritical("MediatorAddress is invalid in appsettings.json");
            return false;
        }

        if(address is null)
        {
            logger.LogCritical("Failed to parse MediatorAddress in appsettings.json");
            return false;
        }

        var portEntry = config["MediatorPort"];
        if(string.IsNullOrEmpty(portEntry))
        {
            logger.LogCritical("MediatorPort not found in appsettings.json");
            return false;
        }

        if (!int.TryParse(portEntry, out int port))
        {
            logger.LogCritical("MediatorPort is invalid in appsettings.json");
            return false;
        }

        mediatorEndpoint.Address = address;
        mediatorEndpoint.Port = port;

        return true;
    }

    private async Task<bool> TryConnectToMediatorAsync()
    {
        try
        {
            await client.ConnectAsync(mediatorEndpoint);
        }
        catch(Exception ex)
        {
            logger.LogCritical($"An exception occured while trying to connect to the mediator server at '{mediatorEndpoint.ToString()}': {ex.Message}");
            return false;
        }

        return true;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Lime Agent Stopped.");
    }
}
