using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Exceptions;

namespace VAH.Backend.Controllers;

/// <summary>
/// Base controller providing common functionality for all API controllers.
/// <list type="bullet">
///   <item><description>Centralizes user identity extraction (string + <see cref="Guid"/>).</description></item>
///   <item><description>Declares universal error response types for Swagger (ProblemDetails).</description></item>
///   <item><description>Provides typed-response helpers to eliminate anonymous objects.</description></item>
/// </list>
/// </summary>
[ApiController]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Get the authenticated user's ID as a string from JWT claims.
    /// Throws <see cref="AuthContextMissingException"/> if identity is not found,
    /// which <see cref="Middleware.GlobalExceptionHandler"/> maps to 401 ProblemDetails.
    /// </summary>
    protected string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new AuthContextMissingException();

    /// <summary>
    /// Get the authenticated user's ID as a <see cref="Guid"/>.
    /// Use when the domain layer expects strongly-typed identifiers.
    /// </summary>
    protected Guid GetUserGuid() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var guid)
            ? guid
            : throw new AuthContextMissingException();
}
