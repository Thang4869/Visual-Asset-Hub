using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

/// <summary>Health check endpoint for monitoring and load balancer readiness.</summary>
/// <remarks>DIP: Delegates all probing logic to <see cref="IHealthCheckService"/>.
/// Returns 503 with the same typed body when degraded, so callers always parse one schema.</remarks>
[Route("api/v1/[controller]")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class HealthController(IHealthCheckService healthCheckService) : BaseApiController
{
    /// <summary>Basic health check — verifies API is running and DB is reachable.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(HealthCheckResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthCheckResult), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealth(CancellationToken ct = default)
    {
        var result = await healthCheckService.CheckAsync(ct);
        return result.IsHealthy ? Ok(result) : StatusCode(503, result);
    }
}
