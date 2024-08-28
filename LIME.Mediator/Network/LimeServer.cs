﻿using LIME.Mediator.Models;
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
    public event EventHandler<EventArgs>? ClientAuthenticationFailed;

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

    public async Task ListenAsync(CancellationToken cancellationToken)
    {
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
        catch (Exception ex)
        {
            ClientAuthenticationFailed?.Invoke(this, new ClientAuthenticationFailedEventArgs($"Failed to validate client certificate: {ex.Message}"));
            return false;
        }
    }

    private async Task HandleAcceptConnectionAsync(TcpClient client)
    {
        try
        {
            var stream = clientCertRequired ? new SslStream(client.GetStream(), false) :
                                                new SslStream(client.GetStream(), false, ValidateClientCertificate);

            var limeClient = new LimeClient(client)
            {
                Guid = Guid.NewGuid(),
                Stream = stream,
                State = LimeClientState.Connecting
            };

            var authenticationResult = await AuthenticateAsync(limeClient);
            if (!authenticationResult.Success)
            {
                ClientAuthenticationFailed?.Invoke(this, new ClientAuthenticationFailedEventArgs(authenticationResult.Message));
                return;
            }
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
            await client.Stream.AuthenticateAsServerAsync(certificate, clientCertRequired, SslProtocols.Tls13, true);

            return new TaskResult(true);
        }
        catch (Exception ex)
        {
            return new TaskResult(false, $"Failed to authenticate client '{client.Socket.RemoteEndPoint}': {ex.Message}");
        }
    }
}
