﻿using LIME.Agent.Configuration;
using LIME.Agent.Network.Packets;
using LIME.Shared.Crypto;
using LIME.Shared.Extensions;
using LIME.Shared.Network;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace LIME.Agent.Services;

public partial class LimeAgent : IHostedService
{
    public bool CheckCertificateRevocation { get; set; }

    public Dictionary<LimeOpCodes, Func<SslStream, Task>> PacketHandlers;

    private TcpClient client;
    private SslStream? stream;
    private bool connected;

    private X509Certificate2? certificate;

    private readonly ILogger<LimeAgent> logger;
    private readonly LimeAgentConfig config;

    public LimeAgent(ILogger<LimeAgent> logger, LimeAgentConfig config)
    {
        this.logger = logger;
        this.config = config;

        LoadClientCertificate(config.Certificate.Thumbprint);

        CheckCertificateRevocation = false;
        PacketHandlers = new Dictionary<LimeOpCodes, Func<SslStream, Task>>()
        {
            { LimeOpCodes.SMSG_DISCONNECT, HandleDisconnectAsync }
        };

        client = new TcpClient();
        connected = false;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Lime Agent Started.");

        if(!await TryConnectToMediatorAsync())
        {
            return;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Lime Agent Stopped.");

        await Task.Delay(1);
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

            await ConnectAsync(config.MediatorHost, new IPEndPoint(address, config.MediatorPort));
        }
        catch(Exception ex)
        {
            logger.LogCritical($"An exception occured while trying to connect to the mediator server at '{config.MediatorAddress.ToString()}:{config.MediatorPort.ToString()}': {ex.Message}");
            return false;
        }

        return true;
    }

    public async Task ConnectAsync(string hostname, IPEndPoint endpoint)
    {
        if (certificate is null)
        {
            throw new NullReferenceException("No client certificate is loaded.");
        }

        await client.ConnectAsync(endpoint.Address, endpoint.Port);

        stream = new SslStream(client.GetStream(), false, ValidateServerCertificate);

        if (CheckCertificateRevocation)
        {
            await stream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions()
            {
                ClientCertificates = new X509CertificateCollection()
                {
                    certificate
                },
                CertificateRevocationCheckMode = X509RevocationMode.Online,
                CertificateChainPolicy = new X509ChainPolicy()
                {
                    RevocationFlag = X509RevocationFlag.EntireChain,
                },
                TargetHost = hostname
            });
        }
        else
        {
            await stream.AuthenticateAsClientAsync(hostname, new X509Certificate2Collection() { certificate }, false);
        }

        logger.LogInformation("Connected to mediator server.");
        connected = true;

        _ = StartListeningForDataAsync();
        _ = StartHeartbeatAsync();
    }

    private async Task StartListeningForDataAsync()
    {
        try
        {
            while (connected)
            {
                if (stream is null)
                {
                    return;
                }

                var packetType = await stream.ReadPacketTypeAsync();

                if (!PacketHandlers.ContainsKey(packetType))
                {
                    logger.LogCritical($"Received invalid opcode '{(int)packetType}'. Disconnected.");
                    connected = false;
                    return;
                }

                await PacketHandlers[packetType].Invoke(stream);
            }
        }
        catch (Exception ex)
        {
            logger.LogCritical($"{ex.Message}: {ex.StackTrace}");
        }
    }

    private async Task StartHeartbeatAsync()
    {
        try
        {
            while (connected)
            {
                if(stream is null)
                {
                    return;
                }

                await Task.Delay(config.HeartbeatFrequency * 1000);

                logger.LogInformation("Sending heartbeat..");

                var packet = new HeartbeatPacket();
                await stream.WriteAsync(packet.Serialize());

                logger.LogInformation("Sent heartbeat.");
            }
        }
        catch(Exception ex)
        {
            logger.LogCritical($"{ex.Message}: {ex.StackTrace}");
        }
    }

    private bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        if (chain is null || certificate is null)
        {
            return false;
        }

        if (sslPolicyErrors == SslPolicyErrors.None)
        {
            return true;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Got ssl policy error: {sslPolicyErrors.ToString()}");

        if (chain is null)
        {
            sb.AppendLine("Chain is null, could not retrieve status.");
            logger.LogCritical(sb.ToString());
            return false;
        }

        foreach (var item in chain.ChainElements)
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

        return false;
    }

    public void LoadClientCertificate(string certificateThumbprint)
    {
        var cert = LimeCertificate.GetCertificate(certificateThumbprint);
        if (cert is null)
        {
            throw new NullReferenceException($"No certificate was found with the thumbprint '{certificateThumbprint}'.");
        }

        certificate = cert;
    }
}
