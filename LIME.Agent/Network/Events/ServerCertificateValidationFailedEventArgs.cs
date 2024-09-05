using System.Net.Security;

using System.Security.Cryptography.X509Certificates;

namespace LIME.Agent.Network.Events;

internal class ServerCertificateValidationFailedEventArgs
{
    public X509Certificate? Certificate { get; set; }
    public X509Chain? Chain { get; set; }

    public SslPolicyErrors SslPolicyErrors { get; set; }

    public ServerCertificateValidationFailedEventArgs(X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        Certificate = certificate;
        Chain = chain;

        SslPolicyErrors = sslPolicyErrors;
    }
}
