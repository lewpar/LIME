using LIME.Mediator.Models;
using LIME.Mediator.Network.Events;

using LIME.Shared.Crypto;
using LIME.Shared.Models;

using System.Net;
using System.Net.Security;
using System.Net.Sockets;

using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace LIME.Mediator.Network;

internal class LimeServer
{
    public event EventHandler<UnhandledExceptionEventArgs>? UnhandledException;
    public event EventHandler<ClientAuthenticatingEventArgs>? ClientAuthenticating;
    public event EventHandler<ClientAuthenticationFailedEventArgs>? ClientAuthenticationFailed;
    public event EventHandler<ClientAuthenticatedEventArgs>? ClientAuthenticated;
    public event EventHandler<EventArgs>? ServerStarted;

    public List<LimeClient> ConnectedClients { get; set; }

    private TcpListener listener;
    private X509Certificate2 certificate;
    private bool clientCertRequired;

    public LimeServer(IPAddress address, int port, string certificateThumbprint, bool clientCertificateRequired = false)
    {
        ConnectedClients = new List<LimeClient>();

        listener = new TcpListener(address, port);
        certificate = GetCertificate(certificateThumbprint);

        clientCertRequired = clientCertificateRequired;
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
        listener.Start();

        ServerStarted?.Invoke(this, new EventArgs());

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
            if (clientCertificate is null)
            {
                return false;
            }

            if (chain is null)
            {
                return false;
            }

            var root = chain.ChainElements.First();
            if (root.Certificate.Thumbprint != certificate.Thumbprint)
            {
                return false;
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task HandleAcceptConnectionAsync(TcpClient client)
    {
        ClientAuthenticating?.Invoke(this, new ClientAuthenticatingEventArgs(client));

        try
        {
            var stream = clientCertRequired ? new SslStream(client.GetStream(), false) :
                                                new SslStream(client.GetStream(), false, ValidateClientCertificate);

            var limeClient = new LimeClient(client, stream)
            {
                Guid = Guid.NewGuid(),
                Stream = stream,
                State = LimeClientState.Handshaking
            };

            var authenticationResult = await AuthenticateAsync(limeClient);
            if (!authenticationResult.Success)
            {
                ClientAuthenticationFailed?.Invoke(this, new ClientAuthenticationFailedEventArgs(client, authenticationResult.Message));
                return;
            }

            ClientAuthenticated?.Invoke(this, new ClientAuthenticatedEventArgs(limeClient));
            ConnectedClients.Add(limeClient);
        }
        catch (Exception ex)
        {
            UnhandledException?.Invoke(this, new UnhandledExceptionEventArgs(ex, false));
        }
    }

    private async Task<TaskResult> AuthenticateAsync(LimeClient client)
    {
        try
        {
            await client.Stream.AuthenticateAsServerAsync(certificate, clientCertRequired, SslProtocols.Tls13, false);

            return new TaskResult(true);
        }
        catch (Exception ex)
        {
            return new TaskResult(false, $"Failed to authenticate client '{client.Socket.Client.RemoteEndPoint}': {ex.Message}");
        }
    }
}
