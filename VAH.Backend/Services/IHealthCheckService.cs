namespace VAH.Backend.Services;

/// <summary>
/// Abstraction for application health-check logic.
/// DIP: Controllers depend on this interface, not on <see cref="Data.AppDbContext"/> directly.
/// </summary>
public interface IHealthCheckService
{
    /// <summary>Run all health probes and return an aggregate result.</summary>
    Task<HealthCheckResult> CheckAsync(CancellationToken ct = default);
}

/// <summary>Aggregate result of all health probes.</summary>
public sealed record HealthCheckResult(
    string Status,
    DateTime Timestamp,
    HealthChecks Checks,
    HealthInfo Info)
{
    /// <summary>Status string indicating all probes passed.</summary>
    public const string Healthy = "healthy";

    /// <summary>Status string indicating at least one probe failed.</summary>
    public const string Degraded = "degraded";

    /// <summary>True when every probe passed.</summary>
    public bool IsHealthy => Status == Healthy;
}

/// <summary>Individual health-check probe results.</summary>
public sealed record HealthChecks(string Database, string Storage);

/// <summary>Runtime metadata surfaced by the health endpoint.</summary>
public sealed record HealthInfo(string Environment, string Version);
