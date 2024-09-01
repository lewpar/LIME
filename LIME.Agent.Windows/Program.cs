using LIME.Agent.Windows.Configuration;
using LIME.Agent.Windows.Services;

using LIME.Shared.Configuration;
using LIME.Shared.Crypto;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Security.Cryptography.X509Certificates;

namespace LIME.Agent.Windows;

internal class Program
{
    static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder();

        await DotEnv.LoadAsync(Environment.CurrentDirectory);

        await ConfigureServicesAsync(builder.Services);

        var app = builder.Build();

        var config = app.Services.GetService<IConfiguration>();
        var limeConfig = app.Services.GetService<LimeAgentConfig>();

        if (config is null || limeConfig is null)
        {
            return;
        }

        config.Bind(limeConfig);

        await app.RunAsync();
    }

    private static async Task ConfigureServicesAsync(IServiceCollection services)
    {
        await ConfigureConfigAsync(services);

        services.AddHostedService<LimeAgent>();
    }

    static async Task ConfigureConfigAsync(IServiceCollection services)
    {
        var config = await LimeAgentConfig.LoadAsync();
        if (config is null)
        {
            config = new LimeAgentConfig();
            await config.SaveAsync();
        }

        await ConfigureCertificateAsync(config);

        services.AddSingleton<LimeAgentConfig>(config);
    }

    static async Task ConfigureCertificateAsync(LimeAgentConfig config)
    {
        if(LimeCertificate.CertificateExists(config.Certificate.Thumbprint))
        {
            return;
        }

        if(!File.Exists(@"agent.pfx"))
        {
            throw new Exception("No agent.pfx certificate found to import.");
        }

        var chain = LimeCertificate.ImportBundledCertificate(@"agent.pfx");
        LimeCertificate.StoreBundledCertificate(chain);

        X509Certificate2? cert = null;
        foreach (var certificate in chain)
        {
            if (!LimeCertificate.IsRootCertificate(certificate) &&
                !LimeCertificate.IsIntermediateCertificate(certificate))
            {
                cert = certificate;
                break;
            }
        }

        if (cert is null)
        {
            throw new Exception("Failed to get agent certificate while importing certificate chain.");
        }

        config.Certificate.Thumbprint = cert.Thumbprint;
        await config.SaveAsync();

        File.Delete(@"agent.pfx");
    }
}
