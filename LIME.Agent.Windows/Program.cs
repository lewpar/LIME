using LIME.Agent.Windows.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LIME.Agent.Windows;

internal class Program
{
    static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddJsonFile(@"./appsettings.json");

        ConfigureServices(builder.Services);

        var app = builder.Build();
        await app.RunAsync();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddHostedService<LimeAgent>();
    }
}
