﻿using LIME.Mediator.Configuration;
using LIME.Mediator.Database;
using LIME.Mediator.Services;

using LIME.Shared.Configuration;
using LIME.Shared.Crypto;

using Microsoft.EntityFrameworkCore;

using System.Net;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
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

        var config = await ConfigureConfigAsync();
        services.AddSingleton<LimeMediatorConfig>(config);

        ConfigureKestrel(builder.WebHost, config);

        services.AddDbContext<LimeDbContext>();

        services.AddHostedService<LimeMediator>();
    }

    static void ConfigureKestrel(IWebHostBuilder builder, LimeMediatorConfig config)
    {
        builder.ConfigureKestrel(options =>
        {
            options.Listen(new IPEndPoint(IPAddress.Parse(config.Dashboard.Listen.IPAddress), config.Dashboard.Listen.Port), listenOptions =>
            {
                var cert = LimeCertificate.GetCertificate(config.Dashboard.Certificate.Thumbprint, StoreName.My);
                if(cert is null)
                {
                    throw new Exception("Failed to get dashboard certificate while configuring Kestrel.");
                }

                listenOptions.UseHttps(cert);
            });
        });
    }

    static async Task<LimeMediatorConfig> ConfigureConfigAsync()
    {
        var config = await LimeMediatorConfig.LoadAsync();
        if(config is null)
        {
            config = new LimeMediatorConfig();
            await config.SaveAsync();
        }

        bool changesMade = false;

        if (string.IsNullOrWhiteSpace(config.Mediator.RootCertificate.Thumbprint) ||
            !LimeCertificate.CertificateExists(config.Mediator.RootCertificate.Thumbprint, StoreName.Root))
        {
            Console.WriteLine("No valid root certificate found.");
            Console.Write("Enter thumbprint for root certificate: ");
            var thumbprint = Console.ReadLine();

            if (thumbprint is null || !LimeCertificate.CertificateExists(thumbprint, StoreName.Root))
            {
                throw new Exception($"A root certificate with the thumbprint '{thumbprint}' does not exist.");
            }

            config.Mediator.RootCertificate.Thumbprint = thumbprint;
            changesMade = true;
        }

        if (string.IsNullOrWhiteSpace(config.Mediator.IntermediateCertificate.Thumbprint) ||
            !LimeCertificate.CertificateExists(config.Mediator.IntermediateCertificate.Thumbprint, StoreName.CertificateAuthority))
        {
            Console.WriteLine("No valid intermediate certificate found.");
            Console.Write("Enter thumbprint for intermediate certificate: ");
            var thumbprint = Console.ReadLine();

            if(thumbprint is null || !LimeCertificate.CertificateExists(thumbprint, StoreName.CertificateAuthority))
            {
                throw new Exception($"A intermediate certificate with the thumbprint '{thumbprint}' does not exist.");
            }

            config.Mediator.IntermediateCertificate.Thumbprint = thumbprint;
            changesMade = true;
        }

        if (!LimeCertificate.CertificateExists(config.Mediator.ServerCertificate.Thumbprint, StoreName.My))
        {
            var intCert = LimeCertificate.GetCertificate(config.Mediator.IntermediateCertificate.Thumbprint, StoreName.CertificateAuthority);
            if (intCert is null)
            {
                throw new Exception("Failed to retrieve intermediate certificate.");
            }

            var cert = LimeCertificate.CreateServerCertificate(intCert, config.Mediator.ServerCertificate.Subject, "http://192.168.0.102/med.crl");

            LimeCertificate.StoreCertificate(cert, StoreName.My, true);

            string crlPath = "./med.crl";

            var crl = new CertificateRevocationListBuilder().Build(intCert, BigInteger.One, DateTimeOffset.Now.AddYears(1), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            File.WriteAllBytes(crlPath, crl);

            config.Mediator.ServerCertificate.Thumbprint = cert.Thumbprint;

            changesMade = true;
        }

        if (!LimeCertificate.CertificateExists(config.Dashboard.Certificate.Thumbprint, StoreName.My))
        {
            var intCert = LimeCertificate.GetCertificate(config.Mediator.IntermediateCertificate.Thumbprint, StoreName.CertificateAuthority);
            if (intCert is null)
            {
                throw new Exception("Failed to retrieve intermediate certificate.");
            }

            var cert = LimeCertificate.CreateServerCertificate(intCert, config.Dashboard.Certificate.Subject, config.Dashboard.Certificate.DNS);

            LimeCertificate.StoreCertificate(cert, StoreName.My, true);

            config.Dashboard.Certificate.Thumbprint = cert.Thumbprint;

            changesMade = true;
        }

        if (changesMade)
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

        app.UseHsts();
        app.UseHttpsRedirection();

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
