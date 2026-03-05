using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace VAH.Backend.Controllers;

/// <summary>
/// Base controller providing common functionality for all API controllers.
/// <list type="bullet">
///   <item><description>Centralizes user identity extraction.</description></item>
///   <item><description>Declares universal error response types for Swagger (ProblemDetails).</description></item>
///   <item><description>Provides typed-response helpers to eliminate anonymous objects.</description></item>
/// </list>
/// </summary>
[ApiController]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Get the authenticated user's ID from JWT claims.
    /// Throws <see cref="UnauthorizedAccessException"/> if identity is not found.
    /// </summary>
    protected string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User identity not found.");
}
