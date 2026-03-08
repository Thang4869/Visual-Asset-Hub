using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

/// <summary>Authentication endpoints for user registration and login.</summary>
/// <remarks>
/// Rate-limited via <see cref="RateLimitPolicies.Fixed"/> to mitigate brute-force.
/// </remarks>
[Route("api/v1/[controller]")]
[EnableRateLimiting(RateLimitPolicies.Fixed)]
[Produces("application/json")]
public sealed class AuthController(IAuthService authService, ILogger<AuthController> logger) : BaseApiController
{
    /// <summary>Register a new user account.</summary>
    /// <remarks>Returns 201 Created per REST semantics (a new resource was created).</remarks>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto dto, CancellationToken ct = default)
    {
        logger.LogInformation(LogEvents.RegisterAttempt, "Registration attempt for {Email}", MaskEmail(dto.Email));
        var result = await authService.RegisterAsync(dto, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Login with email and password. Returns JWT token.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto, CancellationToken ct = default)
    {
        logger.LogInformation(LogEvents.LoginAttempt, "Login attempt for {Email}", MaskEmail(dto.Email));
        return Ok(await authService.LoginAsync(dto, ct));
    }

    /// <summary>Mask email for safe logging — prevents PII leakage.</summary>
    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        return at <= 1 ? "***" : $"{email[0]}***{email[at..]}";
    }
}
