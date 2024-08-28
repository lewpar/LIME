using LIME.Mediator.Database;
using LIME.Mediator.Configuration;
using LIME.Mediator.Models;

using LIME.Shared.Crypto;
using LIME.Shared.Extensions;

using Microsoft.EntityFrameworkCore;

using System.Net;
using System.Net.Security;
using System.Net.Sockets;

using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

using System.Text;

namespace LIME.Mediator.Services;

public partial class LimeGateway : BackgroundService
{
    private TcpListener _listener;
    private readonly LimeMediatorConfig config;
    private readonly ILogger<LimeGateway> logger;
    private readonly IServiceProvider serviceProvider;
    private readonly LimeMediator mediator;

    public LimeGateway(LimeMediatorConfig config, ILogger<LimeGateway> logger,
        IServiceProvider serviceProvider, LimeMediator mediator)
    {
        this.config = config;
        this.logger = logger;
        this.serviceProvider = serviceProvider;
        this.mediator = mediator;
        _listener = new TcpListener(IPAddress.Parse(config.MediatorBindAddress), config.MediatorListenPort);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _listener.Start();

        logger.LogInformation("Listener started, waiting for agents to connect..");

        await AcceptConnectionsAsync(stoppingToken);
    }

    private async Task AcceptConnectionsAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var client = await _listener.AcceptTcpClientAsync();
            _ = HandleAcceptConnectionAsync(client);
        }
    }

    private async Task HandleAcceptConnectionAsync(TcpClient client)
    {
        try
        {
            var limeClient = new LimeClient(client)
            {
                Guid = Guid.NewGuid(),
                Stream = new SslStream(client.GetStream(), false),
                State = LimeClientState.Connecting
            };

            if (!await AuthenticateAsync(limeClient))
            {
                await limeClient.DisconnectAsync("Failed authentication.");
                return;
            }

            var endpoint = limeClient.Socket.RemoteEndPoint as IPEndPoint;
            if (endpoint is null)
            {
                await limeClient.DisconnectAsync("An internal error occured.");
                return;
            }

            var scope = serviceProvider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetService<LimeDbContext>();

            if(dbContext is null)
            {
                logger.LogCritical("Failed to get DbContext service while authenticating client.");
                await limeClient.DisconnectAsync("An internal error occured.");
                return;
            }

            var agent = await dbContext.Agents.FirstOrDefaultAsync(a => a.Address == endpoint.Address.ToString());
            if (agent is null)
            {
                await limeClient.DisconnectAsync("Unauthorized agent.");
                return;
            }

            if (string.IsNullOrWhiteSpace(agent.Key))
            {
                logger.LogCritical($"Client '{limeClient.Socket.RemoteEndPoint}' is missing public key.");
                await limeClient.DisconnectAsync("Missing public key.");
                return;
            }

            logger.LogInformation($"Client '{limeClient.Socket.RemoteEndPoint}' connected, starting handshake.");

            limeClient.State = LimeClientState.Handshaking;

            await SendHandshakeAsync(limeClient);
            await HandleHandshakeAsync(limeClient);
        }
        catch (Exception ex)
        {
            logger.LogCritical($"{ex.Message}: {ex.StackTrace}");
        }
    }

    private async Task<bool> AuthenticateAsync(LimeClient client)
    {
        try
        {
            X509Certificate2? cert = LimeCertificate.GetCertificate(config.Certificate.Thumbprint);
            if (cert is null)
            {
                logger.LogCritical($"Failed to authenticate client '{client.Socket.RemoteEndPoint}': No valid server certificate was found in My store for CurrentUser.");
                return false;
            }

            await client.Stream.AuthenticateAsServerAsync(cert, false, SslProtocols.Tls13, true);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogCritical($"Failed to authenticate client '{client.Socket.RemoteEndPoint}': {ex.Message}");
            return false;
        }
    }

    public async Task SendHandshakeAsync(LimeClient client)
    {
        /*if (client.PublicKey is null)
        {
            return;
        }

        var stream = client.Stream;

        using var rsa = client.PublicKey.ToRSACryptoProvider();

        var data = Encoding.UTF8.GetBytes(client.Guid.ToString());
        var encryptedData = rsa.Encrypt(data, false);

        var handshake = new HandshakePacket(encryptedData);
        await stream.WriteBytesAsync(handshake.Serialize());

        logger.LogInformation($"Sent handshake to client {client.Socket.RemoteEndPoint}.");*/
    }

    public async Task HandleHandshakeAsync(LimeClient client)
    {
        var stream = client.Stream;

        var packetType = await client.Stream.ReadPacketTypeAsync();
        if (packetType != Shared.Network.LimePacketType.CMSG_HANDSHAKE)
        {
            await client.DisconnectAsync("Invalid packet.");
            return;
        }

        var length = await stream.ReadIntAsync();
        var data = await stream.ReadBytesAsync(length);
        var message = Encoding.UTF8.GetString(data);
        var expectedMsg = client.Guid.ToString();

        if (message != expectedMsg)
        {
            logger.LogWarning($"Client '{client.Socket.RemoteEndPoint}' send invalid handshake message '{message}', expected '{expectedMsg}'.");
            await client.DisconnectAsync("Invalid handshake message.");
            return;
        }

        client.State = LimeClientState.Connected;

        logger.LogInformation($"Client {client.Socket.RemoteEndPoint} passed handshake.");

        mediator.ConnectedClients.Add(client);
        _ = mediator.ListenForDataAsync(client);
    }
}
