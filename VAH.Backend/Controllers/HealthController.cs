using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VAH.Backend.Data;

namespace VAH.Backend.Controllers;

/// <summary>
/// Health check endpoint for monitoring and load balancer readiness.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;

    public HealthController(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    /// <summary>
    /// Basic health check — verifies API is running and DB is reachable.
    /// GET /api/health
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
        var dbHealthy = false;
        try
        {
            dbHealthy = await _context.Database.CanConnectAsync();
        }
        catch { /* swallow */ }

        var uploadsPath = Path.Combine(_env.WebRootPath ?? "", "uploads");
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
                environment = _env.EnvironmentName,
                version = typeof(HealthController).Assembly.GetName().Version?.ToString() ?? "1.0.0",
            }
        };

        return dbHealthy ? Ok(result) : StatusCode(503, result);
    }
}
