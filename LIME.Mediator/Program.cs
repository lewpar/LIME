using LIME.Dashboard.Database;
using LIME.Mediator.Configuration;
using LIME.Mediator.Services;

using LIME.Shared.Crypto;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LIME.Mediator;

internal class Program
{
    static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        await ConfigureServicesAsync(builder.Services);

        var app = builder.Build();

        await app.RunAsync();
    }

    static async Task ConfigureServicesAsync(IServiceCollection services)
    {
        await ConfigureConfigAsync(services);

        services.AddDbContext<LimeDbContext>();

        services.AddSingleton<LimeMediator>();
        services.AddHostedService<LimeGateway>();
        services.AddHostedService<LimeHeartbeat>();
    }

    static async Task ConfigureConfigAsync(IServiceCollection services)
    {
        var config = await LimeMediatorConfig.LoadAsync();
        if(config is null)
        {
            config = new LimeMediatorConfig();
            await config.SaveAsync();
        }

        if(string.IsNullOrEmpty(config.CertificateThumbprint))
        {
            var cert = LimeCertificate.CreateCertificate();
            LimeCertificate.StoreCertificate(cert);

            config.CertificateThumbprint = cert.Thumbprint;
            await config.SaveAsync();
        }

        services.AddSingleton<LimeMediatorConfig>(config);
    }
}
