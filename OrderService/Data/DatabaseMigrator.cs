using DbUp;
using System.Reflection;

namespace OrderService.Data;

public static class DatabaseMigrator
{
    public static void MigrateDatabase(string connectionString, ILogger logger)
    {
        logger.LogInformation("Starting database migration...");

        EnsureDatabase.For.PostgresqlDatabase(connectionString);

        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
            .LogTo(new DbUpLogger(logger))
            .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            logger.LogError(result.Error, "Database migration failed");
            throw new InvalidOperationException("Database migration failed", result.Error);
        }

        logger.LogInformation("Database migration completed successfully");
    }

    private class DbUpLogger(ILogger logger) : DbUp.Engine.Output.IUpgradeLog
    {
        public void WriteInformation(string format, params object[] args)
        {
            logger.LogInformation(format, args);
        }

        public void WriteError(string format, params object[] args)
        {
            logger.LogError(format, args);
        }

        public void WriteWarning(string format, params object[] args)
        {
            logger.LogWarning(format, args);
        }
    }
}
