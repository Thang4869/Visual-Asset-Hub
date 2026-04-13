using Microsoft.EntityFrameworkCore;
using VAH.Backend.Data;

namespace VAH.Backend.Extensions;

/// <summary>
/// Applies pending EF Core migrations.
/// </summary>
public sealed class DatabaseMigrationInitializer(
    AppDbContext context,
    ILogger<DatabaseMigrationInitializer> logger) : IStartupInitializer
{
    public int Order => 0;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation("Applying pending EF Core migrations...");

            try
            {
                // Auto-recover from corrupt migrations (e.g. OOM killed during first run leaving tables without __EFMigrationsHistory)
                var tablesExist = false;
                try
                {
                    // Will throw if AspNetRoles doesn't exist yet
                    await context.Database.ExecuteSqlRawAsync("SELECT 1 FROM \"AspNetRoles\" LIMIT 1", ct);
                    tablesExist = true;
                }
                catch { /* Ignore, tables safely don't exist */ }

                if (tablesExist)
                {
                    var migrations = await context.Database.GetAppliedMigrationsAsync(ct);
                    if (!migrations.Any())
                    {
                        logger.LogWarning("Corrupted schema detected (tables exist but no migration history). Wiping public schema to recover...");
                        await context.Database.ExecuteSqlRawAsync("DROP SCHEMA public CASCADE; CREATE SCHEMA public;", ct);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to perform auto-recovery check, continuing with standard migration...");
            }

            await context.Database.MigrateAsync(ct);
            logger.LogInformation("Database migration completed");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Fatal error during database migration: " + ex.Message);
            throw;
        }
    }
}
