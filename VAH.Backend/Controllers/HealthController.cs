using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VAH.Backend.Data;

namespace VAH.Backend.Controllers;

/// <summary>
/// Health check endpoint for monitoring and load balancer readiness.
/// </summary>
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class HealthController(AppDbContext context, IWebHostEnvironment env) : BaseApiController
{
    /// <summary>Basic health check — verifies API is running and DB is reachable.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealth(CancellationToken ct)
    {
        var dbHealthy = false;
        try
        {
            dbHealthy = await context.Database.CanConnectAsync(ct);
        }
        catch { /* swallow */ }

        var uploadsPath = Path.Combine(env.WebRootPath ?? "", "uploads");
        var storageHealthy = Directory.Exists(uploadsPath);

        var result = new
        {
            status = dbHealthy && storageHealthy ? "healthy" : "degraded",
            timestamp = DateTime.UtcNow,
            checks = new
            {
                database = dbHealthy ? "ok" : "unavailable",
                storage = storageHealthy ? "ok" : "unavailable",
            },
            info = new
            {
                environment = env.EnvironmentName,
                version = typeof(HealthController).Assembly.GetName().Version?.ToString() ?? "1.0.0",
            }
        };

        return dbHealthy ? Ok(result) : StatusCode(503, result);
    }
}
