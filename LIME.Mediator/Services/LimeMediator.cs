using LIME.Mediator.Configuration;
using LIME.Mediator.Network;

using LIME.Shared.Crypto;
using LIME.Shared.Extensions;
using LIME.Shared.Models;
using LIME.Shared.Network;

using System.Net;
using System.Net.Security;
using System.Net.Sockets;

using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using LIME.Mediator.Database;
using Microsoft.EntityFrameworkCore;
using LIME.Mediator.Database.Models;
using Microsoft.AspNetCore.Http;

namespace LIME.Mediator.Services;

public partial class LimeMediator : BackgroundService
{
    public List<LimeClient> ConnectedClients { get; set; }
    public Dictionary<LimeOpCodes, Func<LimeClient, Task>> PacketHandlers;

    private readonly LimeMediatorConfig config;
    private readonly ILogger<LimeMediator> logger;
    private readonly IServiceProvider serviceProvider;

    private CancellationToken cancellationToken;

    private TcpListener listener;
    private X509Certificate2 certificate;

    private System.Timers.Timer heartbeatTimer;

    public LimeMediator(LimeMediatorConfig config, ILogger<LimeMediator> logger, IServiceProvider serviceProvider)
    {
        this.logger = logger;
        this.serviceProvider = serviceProvider;
        this.config = config;

        listener = new TcpListener(IPAddress.Parse(config.Mediator.Listen.IPAddress), config.Mediator.Listen.Port);
        certificate = GetCertificate(config.Mediator.ServerCertificate.Thumbprint);

        heartbeatTimer = new System.Timers.Timer(config.Mediator.HeartbeatCheckFrequency * 1000);
        heartbeatTimer.Elapsed += HandleHeartbeatAsync;

        ConnectedClients = new List<LimeClient>();
        PacketHandlers = new Dictionary<LimeOpCodes, Func<LimeClient, Task>>()
        {
            { LimeOpCodes.CMSG_HEARTBEAT, HandleHeartbeatAsync },
            { LimeOpCodes.CMSG_STATISTIC, HandleStatisticAsync },
        };
    }

    private X509Certificate2 GetCertificate(string certificateThumbprint)
    {
        var cert = LimeCertificate.GetCertificate(certificateThumbprint);
        if (cert is null)
        {
            throw new NullReferenceException($"No certificate was found with the thumbprint '{certificateThumbprint}'.");
        }

        return cert;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        cancellationToken = stoppingToken;

        heartbeatTimer.Start();

        await StartListeningAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                var exception = task.Exception;
                if (exception is not null)
                {
                    logger.LogCritical($"{exception.Message}: {exception.StackTrace}");
                }
            }
        });
    }

    private async Task StartListeningAsync()
    {
        listener.Start();

        while (!cancellationToken.IsCancellationRequested)
        {
            var client = await listener.AcceptTcpClientAsync();
            _ = HandleAcceptConnectionAsync(client);
        }
    }

    private bool ValidateClientCertificate(object sender, X509Certificate? clientCertificate, string expectedThumbprint, LimeEndpoint endpoint, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        try
        {
            if(clientCertificate is null)
            {
                return false;
            }

            if(new X509Certificate2(clientCertificate).Thumbprint != expectedThumbprint)
            {
                logger.LogCritical($"Endpoint '{endpoint}' tried connected with invalid thumbprint.");
                return false;
            }

            return sslPolicyErrors == SslPolicyErrors.None;
        }
        catch (Exception ex)
        {
            logger.LogCritical($"{ex.Message}: {ex.StackTrace}");
            return false;
        }
    }

    private async Task HandleAcceptConnectionAsync(TcpClient client)
    {
        try
        {
            var endpoint = new LimeEndpoint("0.0.0.0", 0);

            var ipEndpoint = client.Client.RemoteEndPoint as IPEndPoint;
            if (ipEndpoint is not null)
            {
                endpoint.IPAddress = ipEndpoint.Address.MapToIPv4().ToString();
                endpoint.Port = ipEndpoint.Port;
            }

            using var scope = serviceProvider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetService<LimeDbContext>();
            if (dbContext is null)
            {
                logger.LogCritical($"Failed to get database context instance while validating client certificate for endpoint '{endpoint}'.");
                return;
            }

            var agent = await GetAgentAsync(endpoint);
            if (agent is null)
            {
                logger.LogCritical($"Agent '{endpoint.ToString()}' tried to connect but was not registered.");
                return;
            }

            if(string.IsNullOrWhiteSpace(agent.Thumbprint))
            {
                logger.LogCritical($"No certificate thumbprint was configured for agent '{agent.Guid}'.");
                return;
            }

            var stream = new SslStream(client.GetStream(), false, (sender, certificate, chain, sslPolicyErrors) => {
                return ValidateClientCertificate(sender, certificate, agent.Thumbprint, endpoint, chain, sslPolicyErrors);
            });

            var limeClient = new LimeClient(client, stream)
            {
                Guid = agent.Guid,
                Stream = stream,
                State = LimeClientState.Handshaking,
                Endpoint = endpoint
            };

            logger.LogInformation($"Client '{limeClient.Endpoint}' authenticating..");

            var authenticationResult = await AuthenticateAsync(limeClient);
            if (!authenticationResult.Success)
            {
                return;
            }

            ConnectedClients.Add(limeClient);

            logger.LogInformation($"Client '{limeClient.Endpoint}' authenticated.");

            limeClient.LastHeartbeat = DateTimeOffset.Now;
            limeClient.State = LimeClientState.Connected;

            await UpdateAgentStatusAsync(limeClient, AgentStatus.Online);

            await StartListeningForDataAsync(limeClient);
        }
        catch (Exception ex)
        {
            logger.LogCritical($"{ex.Message}: {ex.StackTrace}");
        }
    }

    private async Task StartListeningForDataAsync(LimeClient client)
    {
        try
        {
            if(client.State != LimeClientState.Connected)
            {
                logger.LogCritical("Started listening for client data when client is not ready.");
                return;
            }

            while (client.State == LimeClientState.Connected)
            {
                var packetType = await client.Stream.ReadEnumAsync<LimeOpCodes>();
                if(packetType is null)
                {
                    await DisconnectClientAsync(client);
                    return;
                }

                if (!PacketHandlers.ContainsKey(packetType.Value))
                {
                    await DisconnectClientAsync(client);
                    return;
                }

                await PacketHandlers[packetType.Value].Invoke(client);
            }
        }
        catch (Exception ex)
        {
            await DisconnectClientAsync(client);
            logger.LogCritical($"{ex.Message}: {ex.StackTrace}");
        }
    }

    private async Task<TaskResult> AuthenticateAsync(LimeClient client)
    {
        try
        {
            await client.Stream.AuthenticateAsServerAsync(certificate, true, SslProtocols.Tls13, false);

            return new TaskResult(true);
        }
        catch (Exception ex)
        {
            return new TaskResult(false, $"Failed to authenticate client '{client.Endpoint}': {ex.Message}");
        }
    }

    private async Task DisconnectClientAsync(LimeClient client, LimeDisconnectReason reason = LimeDisconnectReason.Unknown)
    {
        await client.DisconnectAsync($"{reason.ToString()}");
        logger.LogInformation($"Client '{client.Endpoint}' disconnected: {reason.ToString()}");

        await UpdateAgentStatusAsync(client, AgentStatus.Offline);
    }

    private async Task UpdateAgentStatusAsync(LimeClient client, AgentStatus status)
    {
        using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetService<LimeDbContext>();

        if (dbContext is null)
        {
            return;
        }

        var agent = await dbContext.Agents.FirstOrDefaultAsync(a => a.Address == client.Endpoint.IPAddress);
        if (agent is null)
        {
            return;
        }

        agent.Status = status;

        int rowsChanged = await dbContext.SaveChangesAsync();
        if(rowsChanged < 1)
        {
            logger.LogCritical($"Failed to update agent status for '{client.Guid}'.");
        }
    }

    private async Task<Agent?> GetAgentAsync(LimeEndpoint endpoint)
    {
        using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetService<LimeDbContext>();

        if(dbContext is null)
        {
            return null;
        }

        var agent = await dbContext.Agents.FirstOrDefaultAsync(a => a.Address == endpoint.IPAddress);
        if(agent is null)
        {
            return null;
        }

        return agent;
    }
}
