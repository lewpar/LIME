namespace LIME.Dashboard;

using LIME.Dashboard.Configuration;
using LIME.Dashboard.Database;
using LIME.Shared.Configuration;
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

        services.AddSingleton<LimeDashboardConfig>(config);
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

