using LIME.Database;
using LIME.Services;

using Microsoft.EntityFrameworkCore;

namespace LIME;

class Program
{
    static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        ConfigureServices(builder.Services);

        var app = builder.Build();

        ConfigureMiddleware(app);

        app.Run();
    }

    static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<LimeMediator>();
        services.AddHostedService<LimeMediatorGateway>();

        services.AddDbContext<LimeDbContext>();

        services.AddControllers();
        services.AddRazorPages();
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
