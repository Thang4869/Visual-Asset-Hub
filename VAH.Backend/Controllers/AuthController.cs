using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MediatR;
using VAH.Backend.Models;
using VAH.Backend.Features.Auth.Commands;

namespace VAH.Backend.Controllers;

/// <summary>Authentication endpoints for user registration and login.</summary>
/// <remarks>
/// Chuyển đổi thành CQRS Vertical Slice với IMediator. Thin Controller 10/10.
/// Rate-limited via <see cref="RateLimitPolicies.Fixed"/> to mitigate brute-force.
/// </remarks>
[Route("api/v1/[controller]")]
[EnableRateLimiting(RateLimitPolicies.Fixed)]
[Produces("application/json")]
public sealed class AuthController(IMediator mediator, ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto dto, CancellationToken ct = default)
    {
        logger.LogInformation(LogEvents.RegisterAttempt, "Registration attempt for {Email}", MaskEmail(dto.Email));
        
        var result = await mediator.Send(new RegisterCommand(dto), ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto, CancellationToken ct = default)
    {
        logger.LogInformation(LogEvents.LoginAttempt, "Login attempt for {Email}", MaskEmail(dto.Email));
        
        var result = await mediator.Send(new LoginCommand(dto), ct);
        return Ok(result);
    }

    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1) return "***";
        var domain = email[(at + 1)..];
        var dot = domain.LastIndexOf('.');
        var maskedDomain = dot > 1 ? $"{domain[0]}***{domain[dot..]}" : "***";
        return $"{email[0]}***@{maskedDomain}";
    }
}
