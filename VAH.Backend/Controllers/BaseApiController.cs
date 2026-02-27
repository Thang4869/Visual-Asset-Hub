using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace VAH.Backend.Controllers;

/// <summary>
/// Base controller providing common functionality for all API controllers.
/// Centralizes user identity extraction and shared helpers.
/// </summary>
[ApiController]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Get the authenticated user's ID from JWT claims.
    /// Throws UnauthorizedAccessException if identity is not found.
    /// </summary>
    protected string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User identity not found.");
}
