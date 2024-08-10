using LIME.Mediator.Configuration;
using LIME.Mediator.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LIME.Mediator
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Configuration.AddJsonFile(@"./appsettings.json");

            ConfigureServices(builder.Services);

            var app = builder.Build();

            var config = app.Services.GetService<IConfiguration>();
            var limeConfig = app.Services.GetService<LimeMediatorConfig>();

            if (config is null || limeConfig is null)
            {
                return;
            }

            config.Bind(limeConfig);

            await app.RunAsync();
        }

        static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<LimeMediatorConfig>();

            services.AddSingleton<LimeMediator>();
            services.AddHostedService<LimeGateway>();
            services.AddHostedService<LimeHeartbeat>();
        }
    }
}
