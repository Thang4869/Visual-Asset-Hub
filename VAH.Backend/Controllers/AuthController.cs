using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

/// <summary>
/// Authentication endpoints for user registration and login.
/// </summary>
[Route("api/v1/[controller]")]
[EnableRateLimiting("Fixed")]
[Produces("application/json")]
public class AuthController(IAuthService authService) : BaseApiController
{
    /// <summary>Register a new user account.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto dto, CancellationToken ct)
        => Ok(await authService.RegisterAsync(dto, ct));

    /// <summary>Login with email and password. Returns JWT token.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto, CancellationToken ct)
        => Ok(await authService.LoginAsync(dto, ct));
}
