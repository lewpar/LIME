using LIME.Mediator.Configuration;
using LIME.Mediator.Database;
using LIME.Mediator.Services;

using LIME.Shared.Configuration;
using LIME.Shared.Crypto;
using LIME.Shared.Extensions;

using Microsoft.EntityFrameworkCore;

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

        await ConfigureCertificateAsync(config);

        services.AddSingleton<LimeMediatorConfig>(config);
    }

    static async Task ConfigureCertificateAsync(LimeMediatorConfig config)
    {
        if (!string.IsNullOrEmpty(config.Certificate.Thumbprint) &&
            LimeCertificate.GetCertificate(config.Certificate.Thumbprint) is not null)
        {
            return;
        }

        var result = ConsoleHelper.RequestYesNo("No certificate is configured, would you like to create one?", "yes", "no");
        if (result is null)
        {
            await ConfigureCertificateAsync(config);
            return;
        }

        if(!result.Value)
        {
            Console.Clear();
            return;
        }

        Console.WriteLine("Creating certificate..");

        var cert = LimeCertificate.CreateRootCertificate(config.Certificate.Issuer);
        LimeCertificate.StoreCertificate(cert);

        Console.WriteLine($"Certificated created and stored with thumbprint '{cert.Thumbprint}'.");

        config.Certificate.Thumbprint = cert.Thumbprint;
        await config.SaveAsync();

        ConsoleHelper.RequestEnter();
        Console.Clear();
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
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LimeDbContext>();
            dbContext.Database.Migrate();
        }
    }
}
