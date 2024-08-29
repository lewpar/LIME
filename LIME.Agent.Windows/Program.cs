using LIME.Agent.Windows.Configuration;
using LIME.Agent.Windows.Services;

using LIME.Shared.Configuration;
using LIME.Shared.Crypto;
using LIME.Shared.Extensions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        if (!string.IsNullOrEmpty(config.Certificate.Thumbprint) &&
            LimeCertificate.GetCertificate(config.Certificate.Thumbprint) is not null)
        {
            return;
        }

        var certs = LimeCertificate.GetCertificates();
        var issuerCerts = certs.Where(c => c.Issuer == $"CN={config.Certificate.Issuer}").ToList();
        if (issuerCerts.Count() < 1)
        {
            throw new Exception($"No certificate found, please install a certificate issued by '{config.Certificate.Issuer}'.");
        }

        Console.WriteLine($"Certificate(s) issued by '{config.Certificate.Issuer}' found. Select a certificate to use.");

        int? selection = null;
        while (selection is null)
        {
            for (int i = 0; i < issuerCerts.Count; i++)
            {
                var cert = issuerCerts[i];
                Console.WriteLine($"[{i}] Issuer: {cert.Issuer}, Thumbprint: {cert.Thumbprint}");
            }

            var num = ConsoleHelper.RequestNumber("> ");
            if (num < 0 || num > issuerCerts.Count)
            {
                continue;
            }

            selection = num;
        }

        var selectedCert = issuerCerts[selection.Value];
        Console.WriteLine($"Selected certificate '{selection}' with thumbprint '{selectedCert.Thumbprint}'.");

        config.Certificate.Thumbprint = selectedCert.Thumbprint;
        await config.SaveAsync();

        ConsoleHelper.RequestEnter();
        Console.Clear();
    }
}
