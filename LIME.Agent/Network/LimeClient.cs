using LIME.Agent.Network.Events;

using LIME.Shared.Crypto;

using System.Net;
using System.Net.Security;
using System.Net.Sockets;

using System.Security.Cryptography.X509Certificates;

namespace LIME.Agent.Network;

internal class LimeClient
{
    public event EventHandler<EventArgs> ClientConnected;
    public event EventHandler<ServerCertificateValidationFailedEventArgs>? ServerCertificateValidationFailed;

    private TcpClient client;
    private SslStream? stream;

    private X509Certificate2? certificate;

    public LimeClient()
    {
        client = new TcpClient();
    }

    public async Task ConnectAsync(string hostname, IPEndPoint endpoint)
    {
        if(certificate is null)
        {
            throw new NullReferenceException("No client certificate is loaded.");
        }

        await client.ConnectAsync(endpoint.Address, endpoint.Port);

        stream = new SslStream(client.GetStream(), false, ValidateServerCertificate);

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

        ClientConnected?.Invoke(this, new EventArgs());
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

        ServerCertificateValidationFailed?.Invoke(this, new ServerCertificateValidationFailedEventArgs(certificate, chain, sslPolicyErrors));

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
