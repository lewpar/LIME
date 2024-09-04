using LIME.Agent.Configuration;
using LIME.Shared.Crypto;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System.Net.Security;
using System.Net.Sockets;

using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace LIME.Agent.Services;

internal partial class LimeAgent : IHostedService
{
    private readonly ILogger<LimeAgent> logger;
    private readonly LimeAgentConfig config;

    private X509Certificate2 certificate;

    private TcpClient client;

    public LimeAgent(ILogger<LimeAgent> logger, LimeAgentConfig config)
    {
        this.logger = logger;
        this.config = config;

        certificate = GetCertificate(config.Certificate.Thumbprint);

        client = new TcpClient();
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
            await client.ConnectAsync(config.MediatorAddress, config.MediatorPort);

            var stream = new SslStream(client.GetStream(), false, ValidateServerCertificate);

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
                TargetHost = config.MediatorHost
            });
        }
        catch(Exception ex)
        {
            logger.LogCritical($"An exception occured while trying to connect to the mediator server at '{config.MediatorAddress.ToString()}:{config.MediatorPort.ToString()}': {ex.Message}");
            return false;
        }

        return true;
    }

    private bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        if(chain is null || certificate is null)
        {
            return false;
        }

        if(sslPolicyErrors == SslPolicyErrors.None)
        {
            return true;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Got ssl policy error: {sslPolicyErrors.ToString()}");

        if (chain is null)
        {
            sb.AppendLine("Chain is null, could not retrieve status.");
            return false;
        }

        foreach (var item in chain.ChainElements)
        {
            if(item.ChainElementStatus.Length > 0)
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

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Lime Agent Stopped.");

        await Task.Delay(1);
    }
}
