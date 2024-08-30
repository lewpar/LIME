using LIME.Mediator.Configuration;
using LIME.Mediator.Database;
using LIME.Mediator.Services;

using LIME.Shared.Configuration;
using LIME.Shared.Crypto;
using LIME.Shared.Extensions;

using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace LIME.Mediator;

internal class Program
{
    static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        await DotEnv.LoadAsync(Environment.CurrentDirectory);

        DotEnv.Ensure("MYSQL_CONNECTION");

        await ConfigureServicesAsync(builder, builder.Services);

        var app = builder.Build();

        ConfigureMiddleware(app);

        await app.RunAsync();
    }

    static async Task ConfigureServicesAsync(WebApplicationBuilder builder, IServiceCollection services)
    {
        services.AddRazorPages();

        var config = await ConfigureConfigAsync(services);
        services.AddSingleton<LimeMediatorConfig>(config);

        ConfigureKestrel(builder.WebHost, config);

        services.AddDbContext<LimeDbContext>();

        services.AddSingleton<LimeMediator>();
        services.AddHostedService<LimeGateway>();
    }

    static void ConfigureKestrel(IWebHostBuilder builder, LimeMediatorConfig config)
    {
        builder.ConfigureKestrel(options =>
        {
            options.Listen(new IPEndPoint(IPAddress.Parse(config.DashboardBindAddress), config.DashboardListenPort), listenOptions =>
            {
                var cert = LimeCertificate.GetCertificate(config.ServerCertificate.Thumbprint, StoreName.My);
                if(cert is null)
                {
                    throw new Exception("Failed to get server certificate while configuring Kestrel.");
                }

                listenOptions.UseHttps(cert);
            });
        });
    }

    static async Task<LimeMediatorConfig> ConfigureConfigAsync(IServiceCollection services)
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

        if (!LimeCertificate.CertificateExists(config.IntermediateCertificate.Thumbprint, StoreName.CertificateAuthority))
        {
            var rootCert = LimeCertificate.GetCertificate(config.RootCertificate.Thumbprint, StoreName.Root);
            if (rootCert is null)
            {
                throw new Exception("Failed to retrieve root certificate.");
            }

            var cert = LimeCertificate.CreateIntermediateCertificate(rootCert, config.IntermediateCertificate.Subject);
            cert.Store(StoreName.CertificateAuthority);

            config.IntermediateCertificate.Thumbprint = cert.Thumbprint;

            changesMade = true;
        }

        if (!LimeCertificate.CertificateExists(config.ServerCertificate.Thumbprint, StoreName.My))
        {
            var intCert = LimeCertificate.GetCertificate(config.IntermediateCertificate.Thumbprint, StoreName.CertificateAuthority);
            if(intCert is null)
            {
                throw new Exception("Failed to retrieve intermediate certificate.");
            }

            var cert = LimeCertificate.CreateSignedCertificate(intCert, config.ServerCertificate.Subject, X509CertificateAuthRole.Server);
            cert.Store(StoreName.My);

            config.ServerCertificate.Thumbprint = cert.Thumbprint;

            changesMade = true;
        }

        if(changesMade)
        {
            await config.SaveAsync();
        }

        return config;
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
