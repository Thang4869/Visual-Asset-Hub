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
        logger.LogInformation("Applying pending EF Core migrations...");
        await context.Database.MigrateAsync(ct);
        logger.LogInformation("Database migration completed");
    }
}
