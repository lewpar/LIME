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

        bool changesMade = false;

        if(!LimeCertificate.CertificateExists(config.RootCertificate.Thumbprint, StoreName.Root))
        {
            var cert = LimeCertificate.CreateRootCertificate(config.RootCertificate.Issuer);
            cert.Store(StoreName.Root);

            config.RootCertificate.Thumbprint = cert.Thumbprint;

            changesMade = true;
        }

        if (!LimeCertificate.CertificateExists(config.ServerCertificate.Thumbprint))
        {
            var rootCert = LimeCertificate.GetCertificate(config.RootCertificate.Thumbprint, StoreName.Root);
            if(rootCert is null)
            {
                throw new Exception("Failed to retrieve root certificate.");
            }

            var cert = LimeCertificate.CreateIntermediateCertificate(rootCert, config.ServerCertificate.Subject, X509CertificateAuthRole.Server);
            cert.Store(StoreName.My);

            config.ServerCertificate.Thumbprint = cert.Thumbprint;

            changesMade = true;
        }

        if(changesMade)
        {
            await config.SaveAsync();
        }

        services.AddSingleton<LimeMediatorConfig>(config);
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
