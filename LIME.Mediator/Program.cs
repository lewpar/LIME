using LIME.Mediator.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LIME.Mediator
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            ConfigureServices(builder.Services);

            var host = builder.Build();

            await host.RunAsync();
        }

        static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<LimeMediator>();
            services.AddHostedService<LimeGateway>();
            services.AddHostedService<LimeHeartbeat>();
        }
    }
}
