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

    public LimeMediator(LimeMediatorConfig config, ILogger<LimeMediator> logger)
    {
        this.logger = logger;
        this.config = config;

        listener = new TcpListener(IPAddress.Parse(config.Mediator.Listen.IPAddress), config.Mediator.Listen.Port);
        certificate = GetCertificate(config.Mediator.ServerCertificate.Thumbprint);

        ConnectedClients = new List<LimeClient>();
        PacketHandlers = new Dictionary<LimeOpCodes, Func<LimeClient, Task>>()
        {
            { LimeOpCodes.CMSG_HEARTBEAT, HandleHeartbeatAsync }
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

        await StartServerAsync(stoppingToken);
    }

    private async Task StartServerAsync(CancellationToken cancellationToken)
    {
        this.cancellationToken = cancellationToken;

        listener.Start();

        _ = StartListeningAsync();
        _ = StartListeningForHeartbeatAsync();
    }

    private async Task StartListeningAsync()
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = HandleAcceptConnectionAsync(client);
            }
        }
        catch (Exception ex)
        {
            logger.LogCritical($"{ex.Message}: {ex.StackTrace}");
        }
    }

    private async Task StartListeningForHeartbeatAsync()
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(config.Mediator.HeartbeatCheckFrequency * 1000);

                if (ConnectedClients.Count < 1)
                {
                    continue;
                }

                var now = DateTimeOffset.Now;
                foreach (var client in ConnectedClients)
                {
                    var diff = now - client.LastHeartbeat;
                    if (diff.TotalSeconds > (config.Mediator.HeartbeatTimeout + config.Mediator.HeartbeatTimeoutMargin))
                    {
                        await DisconnectClientAsync(client, LimeDisconnectReason.Timeout);
                    }
                }

                // Cleanup disconnected clients.
                ConnectedClients.RemoveAll(client => client.State == LimeClientState.Disconnected);
            }
        }
        catch (Exception ex)
        {
            logger.LogCritical($"{ex.Message}: {ex.StackTrace}");
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

            var limeClient = new LimeClient(client, stream)
            {
                Guid = Guid.NewGuid(),
                Stream = stream,
                State = LimeClientState.Handshaking
            };

            logger.LogInformation($"Client '{limeClient.Guid}' authenticating..");

            var authenticationResult = await AuthenticateAsync(limeClient);
            if (!authenticationResult.Success)
            {
                return;
            }

            ConnectedClients.Add(limeClient);

            logger.LogInformation($"Client '{limeClient.Guid}' authenticated.");

            limeClient.LastHeartbeat = DateTimeOffset.Now;

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
            while (client.State == LimeClientState.Connected)
            {
                var packetType = await client.Stream.ReadPacketTypeAsync();

                if (!PacketHandlers.ContainsKey(packetType))
                {
                    await DisconnectClientAsync(client);
                    return;
                }

                await PacketHandlers[packetType].Invoke(client);
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
            return new TaskResult(false, $"Failed to authenticate client '{client.Socket.Client.RemoteEndPoint}': {ex.Message}");
        }
    }

    private async Task DisconnectClientAsync(LimeClient client, LimeDisconnectReason reason = LimeDisconnectReason.Unknown)
    {
        await client.DisconnectAsync($"{reason.ToString()}");
        logger.LogInformation($"Client '{client.Guid}' disconnected: {reason.ToString()}");
    }
}
