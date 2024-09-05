using LIME.Agent.Configuration;
using LIME.Agent.Network;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System.Net;

using System.Text;

namespace LIME.Agent.Services;

internal partial class LimeAgent : IHostedService
{
    private LimeClient client;

    private readonly ILogger<LimeAgent> logger;
    private readonly LimeAgentConfig config;

    public LimeAgent(ILogger<LimeAgent> logger, LimeAgentConfig config)
    {
        client = new LimeClient();
        client.LoadClientCertificate(config.Certificate.Thumbprint);

        client.ClientConnected += Client_ClientConnected;
        client.ServerCertificateValidationFailed += Client_ServerCertificateValidationFailed;

        this.logger = logger;
        this.config = config;
    }

    private void Client_ClientConnected(object? sender, EventArgs e)
    {
        logger.LogInformation("Connected to mediator server.");
    }

    private void Client_ServerCertificateValidationFailed(object? sender, Network.Events.ServerCertificateValidationFailedEventArgs e)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Got ssl policy error: {e.SslPolicyErrors.ToString()}");

        if (e.Chain is null)
        {
            sb.AppendLine("Chain is null, could not retrieve status.");
            logger.LogCritical(sb.ToString());
            return;
        }

        foreach (var item in e.Chain.ChainElements)
        {
            if (item.ChainElementStatus.Length > 0)
            {
                sb.AppendLine($"   {item.Certificate.Subject}");
            }

            foreach (var status in item.ChainElementStatus)
            {
                sb.AppendLine($"       {status.Status}: {status.StatusInformation}");
            }
        }

        logger.LogCritical(sb.ToString());
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Lime Agent Started.");

        if(!await TryConnectToMediatorAsync())
        {
            return;
        }
    }

    private async Task<bool> TryConnectToMediatorAsync()
    {
        try
        {
            if(!IPAddress.TryParse(config.MediatorAddress, out IPAddress? address))
            {
                logger.LogCritical("Failed to parse IP address for mediator.");
                return false;
            }

            await client.ConnectAsync(config.MediatorHost, new IPEndPoint(address, config.MediatorPort));
        }
        catch(Exception ex)
        {
            logger.LogCritical($"An exception occured while trying to connect to the mediator server at '{config.MediatorAddress.ToString()}:{config.MediatorPort.ToString()}': {ex.Message}");
            return false;
        }

        return true;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Lime Agent Stopped.");

        await Task.Delay(1);
    }
}
