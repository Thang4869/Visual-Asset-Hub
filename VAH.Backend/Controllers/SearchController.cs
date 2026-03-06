using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

/// <summary>Server-side search endpoint.</summary>
/// <remarks>
/// Replaces client-side <c>.includes()</c> filtering for better performance and scalability.
/// Query parameters are grouped in <see cref="SearchRequestParams"/> for cohesion.
/// Rate-limited to prevent search abuse and reduce database load.
/// </remarks>
[Route("api/v1/[controller]")]
[Authorize]
[EnableRateLimiting(RateLimitPolicies.Search)]
[Produces("application/json")]
public sealed class SearchController(ISearchService searchService) : BaseApiController
{
    /// <summary>Search assets and collections by name/tags with filtering and pagination.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(SearchResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchResult>> Search(
        [FromQuery] SearchRequestParams request,
        CancellationToken ct = default)
        => Ok(await searchService.SearchAsync(
            GetUserId(), request.Query, request.Type,
            request.CollectionId, request.Page, request.PageSize, ct));
}
