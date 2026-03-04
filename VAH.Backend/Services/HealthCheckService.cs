using Microsoft.EntityFrameworkCore;
using VAH.Backend.Data;

namespace VAH.Backend.Services;

/// <summary>
/// Concrete health-check service — probes database connectivity and storage availability.
/// SRP: Encapsulates all health-check logic previously inlined in <c>HealthController</c>.
/// </summary>
public sealed class HealthCheckService(
    AppDbContext context,
    IWebHostEnvironment env,
    ILogger<HealthCheckService> logger) : IHealthCheckService
{
    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckAsync(CancellationToken ct = default)
    {
        var dbHealthy = await CheckDatabaseAsync(ct);
        var storageHealthy = CheckStorage();

        var status = dbHealthy && storageHealthy
            ? HealthCheckResult.Healthy
            : HealthCheckResult.Degraded;

        if (!dbHealthy || !storageHealthy)
            logger.LogWarning("Health check degraded — DB: {DbStatus}, Storage: {StorageStatus}",
                dbHealthy ? "ok" : "unavailable",
                storageHealthy ? "ok" : "unavailable");

        return new HealthCheckResult(
            Status: status,
            Timestamp: DateTime.UtcNow,
            Checks: new HealthChecks(
                Database: dbHealthy ? "ok" : "unavailable",
                Storage: storageHealthy ? "ok" : "unavailable"),
            Info: new HealthInfo(
                Environment: env.EnvironmentName,
                Version: typeof(HealthCheckService).Assembly.GetName().Version?.ToString() ?? "1.0.0"));
    }

    private async Task<bool> CheckDatabaseAsync(CancellationToken ct)
    {
        try
        {
            return await context.Database.CanConnectAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database health probe failed");
            return false;
        }
    }

    private bool CheckStorage()
    {
        var uploadsPath = Path.Combine(env.WebRootPath ?? "", "uploads");
        return Directory.Exists(uploadsPath);
    }
}
