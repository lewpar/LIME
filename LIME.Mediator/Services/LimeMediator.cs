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

namespace LIME.Mediator.Services;

public partial class LimeMediator : BackgroundService
{
    public List<LimeClient> ConnectedClients { get; set; }
    public Dictionary<LimeOpCodes, Func<LimeClient, Task>> PacketHandlers;

    private readonly LimeMediatorConfig config;
    private readonly ILogger<LimeMediator> logger;

    private CancellationToken cancellationToken;

    private TcpListener listener;
    private X509Certificate2 certificate;

    private System.Timers.Timer heartbeatTimer;

    public LimeMediator(LimeMediatorConfig config, ILogger<LimeMediator> logger)
    {
        this.logger = logger;
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

    private bool ValidateClientCertificate(object sender, X509Certificate? clientCertificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        try
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            return false;
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
            var stream = new SslStream(client.GetStream(), false, ValidateClientCertificate);

            var endpoint = new LimeEndpoint("0.0.0.0", 0);
            var ipEndpoint = client.Client.RemoteEndPoint as IPEndPoint;

            if (ipEndpoint is not null)
            {
                endpoint.IPAddress = ipEndpoint.Address.MapToIPv4().ToString();
                endpoint.Port = ipEndpoint.Port;
            }

            var limeClient = new LimeClient(client, stream)
            {
                Guid = Guid.NewGuid(),
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
    }
}
