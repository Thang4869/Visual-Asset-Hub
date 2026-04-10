using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MediatR;
using System.Security.Claims;
using VAH.Backend.Models;
using VAH.Backend.Services;
using VAH.Backend.Features.Search.Queries;

namespace VAH.Backend.Controllers;

/// <summary>Server-side search endpoint.</summary>
/// <remarks>
/// Áp dụng cơ chế CQRS (MediatR), tách rời logic query khỏi Service Tầng trung gian.
/// Rate-limited to prevent search abuse and reduce database load.
/// </remarks>
[Route("api/v1/[controller]")]
[Authorize]
[EnableRateLimiting(RateLimitPolicies.Search)]
[Produces("application/json")]
public sealed class SearchController(IMediator mediator) : ControllerBase
{
    /// <summary>Global Search cho thanh Navigation Bar</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<GlobalSearchResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string query,
        [FromQuery] string? type,
        [FromQuery] int? collectionId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        
        var request = new GlobalSearchQuery(userId, query, type, collectionId, page, pageSize);
        var result = await mediator.Send(request, ct);
        
        return Ok(result);
    }
}
