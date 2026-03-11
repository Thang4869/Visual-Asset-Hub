using Microsoft.EntityFrameworkCore;
using VAH.Backend.Data;

namespace VAH.Backend.Extensions;

/// <summary>
/// Applies pending EF Core migrations. Registered only in Development.
/// </summary>
public sealed class DatabaseMigrationInitializer(
    AppDbContext context,
    ILogger<DatabaseMigrationInitializer> logger) : IStartupInitializer
{
    public int Order => 0;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Applying pending EF Core migrations (Development mode)...");
        await context.Database.MigrateAsync(ct);
        logger.LogInformation("Database migration completed");
    }
}
