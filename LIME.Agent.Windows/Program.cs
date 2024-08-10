using LIME.Agent.Windows.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LIME.Agent.Windows;

internal class Program
{
    static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder();

        ConfigureServices(builder.Services);

        var app = builder.Build();
        await app.RunAsync();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddHostedService<LimeAgent>();
    }
}
