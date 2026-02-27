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
public class SearchController : BaseApiController
{
    private readonly ISearchService _searchService;

    public SearchController(ISearchService searchService)
    {
        _searchService = searchService;
    }

    /// <summary>
    /// Search assets and collections by name/tags.
    /// GET /api/search?q=landscape&type=image&collectionId=1&page=1&pageSize=50
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<SearchResult>> Search(
        [FromQuery] string? q,
        [FromQuery] string? type,
        [FromQuery] int? collectionId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _searchService.SearchAsync(GetUserId(), q, type, collectionId, page, pageSize);
        return Ok(result);
    }
}
