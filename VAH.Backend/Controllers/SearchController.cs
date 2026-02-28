using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

/// <summary>
/// Server-side search endpoint.
/// Replaces client-side .includes() filtering for better performance and scalability.
/// </summary>
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class SearchController(ISearchService searchService) : BaseApiController
{
    /// <summary>Search assets and collections by name/tags with filtering and pagination.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(SearchResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchResult>> Search(
        [FromQuery] string? q,
        [FromQuery] string? type,
        [FromQuery] int? collectionId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
        => Ok(await searchService.SearchAsync(GetUserId(), q, type, collectionId, page, pageSize, ct));
}
