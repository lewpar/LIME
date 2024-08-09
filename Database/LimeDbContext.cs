using LIME.Database.Tables;

using Microsoft.EntityFrameworkCore;

namespace LIME.Database;

public class LimeDbContext : DbContext
{
    private readonly ILogger<LimeDbContext> logger;
    private readonly IConfiguration config;

    public DbSet<Agents> Agents { get; set; }
    public DbSet<AgentsPending> AgentsPending { get; set; }

    public LimeDbContext(DbContextOptions options, 
        ILogger<LimeDbContext> logger, IConfiguration config) : base(options) 
    {
        this.logger = logger;
        this.config = config;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        string? mySqlConn = config.GetConnectionString("MySql");

        if (string.IsNullOrEmpty(mySqlConn))
        {
            logger.LogCritical("MySql Connection String was null or empty.");
            return;
        }

        ServerVersion? mySqlVersion = ServerVersion.AutoDetect(mySqlConn);
        if (mySqlVersion is null)
        {
            logger.LogCritical("Failed to auto-detect MySql version.");
            return;
        }

        builder.UseMySql(mySqlConn, mySqlVersion);
    }
}
