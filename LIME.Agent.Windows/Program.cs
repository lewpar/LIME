using LIME.Agent.Windows.Configuration;
using LIME.Agent.Windows.Services;
using LIME.Shared.Configuration;
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
        services.AddSingleton<LimeAgentConfig>();
    }

    static async Task ConfigureConfigAsync(IServiceCollection services)
    {
        var config = await LimeAgentConfig.LoadAsync();
        if (config is null)
        {
            config = new LimeAgentConfig();
            await config.SaveAsync();
        }

        services.AddSingleton<LimeAgentConfig>(config);
    }
}
