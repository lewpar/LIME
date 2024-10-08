﻿namespace LIME.Mediator.Database;

using LIME.Mediator.Configuration;
using LIME.Mediator.Database.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class LimeDbContext : DbContext
{
    private readonly ILogger<LimeDbContext> logger;
    private readonly LimeMediatorConfig config;

    public DbSet<Agent> Agents { get; set; }

    public LimeDbContext(DbContextOptions options,
        ILogger<LimeDbContext> logger, LimeMediatorConfig config) : base(options)
    {
        this.logger = logger;
        this.config = config;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        try
        {
            var connectionString = Environment.GetEnvironmentVariable("MYSQL_CONNECTION", EnvironmentVariableTarget.Process);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                logger.LogCritical("Failed to get MySql connection string from MYSQL_CONNECTION environment variable.");
                return;
            }

            ServerVersion? mySqlVersion = ServerVersion.AutoDetect(connectionString);
            if (mySqlVersion is null)
            {
                logger.LogCritical("Failed to auto-detect MySql version.");
                return;
            }

            builder.UseMySql(connectionString, mySqlVersion);
        }
        catch(Exception ex)
        {
            logger.LogCritical($"Failed to configure MySql with error: {ex.Message}");
        }
    }
}

