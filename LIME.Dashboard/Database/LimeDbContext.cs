﻿namespace LIME.Dashboard.Database;

using LIME.Dashboard.Configuration;
using LIME.Shared.Database.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class LimeDbContext : DbContext
{
    private readonly ILogger<LimeDbContext> logger;
    private readonly LimeDashboardConfig config;

    public DbSet<Agent> Agents { get; set; }

    public LimeDbContext(DbContextOptions options,
        ILogger<LimeDbContext> logger, LimeDashboardConfig config) : base(options)
    {
        this.logger = logger;
        this.config = config;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        string connectionString = $"Server={config.MySql.Host};Database={config.MySql.Database};Uid={config.MySql.User};Pwd={config.MySql.Pass};";

        ServerVersion? mySqlVersion = ServerVersion.AutoDetect(connectionString);
        if (mySqlVersion is null)
        {
            logger.LogCritical("Failed to auto-detect MySql version.");
            return;
        }

        builder.UseMySql(connectionString, mySqlVersion);
    }
}

