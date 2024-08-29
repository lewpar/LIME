using LIME.Mediator.Configuration;
using LIME.Mediator.Database;
using LIME.Mediator.Services;

using LIME.Shared.Configuration;
using LIME.Shared.Crypto;
using LIME.Shared.Extensions;

using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;

namespace LIME.Mediator;

internal class Program
{
    static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        await DotEnv.LoadAsync(Environment.CurrentDirectory);

        DotEnv.Ensure("MYSQL_CONNECTION");

        await ConfigureServicesAsync(builder.Services);

        var app = builder.Build();

        ConfigureMiddleware(app);

        await app.RunAsync();
    }

    static async Task ConfigureServicesAsync(IServiceCollection services)
    {
        services.AddRazorPages();

        await ConfigureConfigAsync(services);

        services.AddDbContext<LimeDbContext>();

        services.AddSingleton<LimeMediator>();
        services.AddHostedService<LimeGateway>();
    }

    static async Task ConfigureConfigAsync(IServiceCollection services)
    {
        var config = await LimeMediatorConfig.LoadAsync();
        if(config is null)
        {
            config = new LimeMediatorConfig();
            await config.SaveAsync();
        }

        if(!LimeCertificate.CertificateExists(config.RootCertificate.Thumbprint, StoreName.Root))
        {
            await ConfigureRootCertificateAsync(config);
        }

        if (!LimeCertificate.CertificateExists(config.ServerCertificate.Thumbprint))
        {
            await ConfigureServerCertificateAsync(config);
        }

        services.AddSingleton<LimeMediatorConfig>(config);
    }

    static async Task ConfigureRootCertificateAsync(LimeMediatorConfig config)
    {
        var result = ConsoleHelper.RequestYesNo("No root certificate is configured, would you like to create one?", "yes", "no");
        if (result is null)
        {
            await ConfigureRootCertificateAsync(config);
            return;
        }

        if(!result.Value)
        {
            Console.Clear();
            return;
        }

        Console.WriteLine("Creating root certificate..");

        var cert = LimeCertificate.CreateRootCertificate(config.RootCertificate.Issuer);
        LimeCertificate.StoreCertificate(cert, StoreName.Root);

        Console.WriteLine($"Root certificated created and stored with thumbprint '{cert.Thumbprint}'.");

        config.RootCertificate.Thumbprint = cert.Thumbprint;
        await config.SaveAsync();

        ConsoleHelper.RequestEnter();
    }

    static async Task ConfigureServerCertificateAsync(LimeMediatorConfig config)
    {
        var result = ConsoleHelper.RequestYesNo("No server certificate is configured, would you like to create one?", "yes", "no");
        if (result is null)
        {
            await ConfigureServerCertificateAsync(config);
            return;
        }

        if (!result.Value)
        {
            Console.Clear();
            return;
        }

        Console.WriteLine("Creating server certificate..");

        var rootCert = LimeCertificate.GetCertificate(config.RootCertificate.Thumbprint, StoreName.Root);
        if(rootCert is null)
        {
            Console.WriteLine("Failed to create server certificate, could not retrieve root certificate.");
            return;
        }

        var cert = LimeCertificate.CreateIntermediateCertificate(rootCert, "LIME.MEDIATOR", X509CertificateAuthRole.Server);
        LimeCertificate.StoreCertificate(cert, StoreName.My);

        Console.WriteLine($"Server certificated created and stored with thumbprint '{cert.Thumbprint}'.");

        config.ServerCertificate.Thumbprint = cert.Thumbprint;
        await config.SaveAsync();

        ConsoleHelper.RequestEnter();
    }

    static void ConfigureMiddleware(WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }

        RunDatabaseMigrations(app);

        app.UseStaticFiles();

        app.UseRouting();

        app.MapRazorPages();
    }

    static void RunDatabaseMigrations(WebApplication app)
    {
        try
        {
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<LimeDbContext>();

                dbContext.Database.Migrate();
            }
        }
        catch(Exception ex)
        {
            app.Logger.LogCritical($"Failed to run database migrations with error: {ex.Message}");
        }
    }
}
