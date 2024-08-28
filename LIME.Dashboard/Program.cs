namespace LIME.Dashboard;

using LIME.Dashboard.Configuration;
using LIME.Dashboard.Database;
using LIME.Shared.Configuration;
using LIME.Shared.Crypto;
using LIME.Shared.Extensions;
using Microsoft.EntityFrameworkCore;

class Program
{
    static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        await DotEnv.LoadAsync(Environment.CurrentDirectory);

        DotEnv.Ensure("MYSQL_CONNECTION");

        await ConfigureServicesAsync(builder.Services);

        var app = builder.Build();

        ConfigureMiddleware(app);

        app.Run();
    }

    static async Task ConfigureServicesAsync(IServiceCollection services)
    {
        await ConfigureConfigAsync(services);

        services.AddDbContext<LimeDbContext>();

        services.AddControllers();
        services.AddRazorPages();
    }

    static async Task ConfigureConfigAsync(IServiceCollection services)
    {
        var config = await LimeDashboardConfig.LoadAsync();
        if (config is null)
        {
            config = new LimeDashboardConfig();
            await config.SaveAsync();
        }

        await ConfigureCertificateAsync(config);

        services.AddSingleton<LimeDashboardConfig>(config);
    }

    static async Task ConfigureCertificateAsync(LimeDashboardConfig config)
    {
        if (!string.IsNullOrEmpty(config.Certificate.Thumbprint) &&
            LimeCertificate.GetCertificate(config.Certificate.Thumbprint) is not null)
        {
            return;
        }

        var certs = LimeCertificate.GetCertificates();
        var issuerCerts = certs.Where(c => c.Issuer == $"CN={config.Certificate.Issuer}").ToList();
        if(issuerCerts.Count() < 1)
        {
            throw new Exception($"No certificate found, please install a certificate issued by '{config.Certificate.Issuer}'.");
        }

        Console.WriteLine($"Certificate(s) issued by '{config.Certificate.Issuer}' found. Select a certificate to use.");

        int? selection = null;
        while(selection is null)
        {
            for(int i = 0; i < issuerCerts.Count; i++)
            {
                var cert = issuerCerts[i];
                Console.WriteLine($"[{i}] Issuer: {cert.Issuer}, Thumbprint: {cert.Thumbprint}");
            }

            var num = ConsoleHelper.RequestNumber("> ");
            if(num < 0 || num > issuerCerts.Count)
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

    static void ConfigureMiddleware(WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }

        RunDatabaseMigrations(app);

        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
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

