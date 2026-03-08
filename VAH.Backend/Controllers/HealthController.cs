using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

/// <summary>Health check endpoints for monitoring and load balancer probes.</summary>
/// <remarks>
/// DIP: Delegates all probing logic to <see cref="IHealthCheckService"/>.
/// Returns 503 with the same typed body when degraded, so callers always parse one schema.
/// <para>Provides both a combined readiness check (<c>/health</c>) and a lightweight
/// liveness probe (<c>/health/live</c>) for Kubernetes-style orchestrators.</para>
/// <para>All responses include <c>Cache-Control: no-store</c> to prevent stale health data.</para>
/// </remarks>
[Route("api/v1/[controller]")]
[AllowAnonymous]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
[ResponseCache(NoStore = true)]
public sealed class HealthController(IHealthCheckService healthCheckService) : ControllerBase
{
    private static readonly string AppVersion = Assembly.GetEntryAssembly()?
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
        ?? "unknown";

    /// <summary>Readiness check — verifies API is running, DB reachable, and storage available.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(HealthCheckResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthCheckResult), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealth(CancellationToken ct = default)
    {
        var result = await healthCheckService.CheckAsync(ct);
        return result.IsHealthy ? Ok(result) : StatusCode(503, result);
    }

    /// <summary>Liveness probe — lightweight check that the process is running.</summary>
    /// <remarks>Does not probe external dependencies (DB, storage). Use for K8s livenessProbe.
    /// Includes application version for operational debugging.</remarks>
    [HttpGet("live")]
    [ProducesResponseType(typeof(LivenessResult), StatusCodes.Status200OK)]
    public IActionResult GetLiveness()
        => Ok(new LivenessResult("alive", DateTime.UtcNow, AppVersion));
}

/// <summary>Minimal liveness response — no external dependency probing.</summary>
public sealed record LivenessResult(string Status, DateTime Timestamp, string Version);
