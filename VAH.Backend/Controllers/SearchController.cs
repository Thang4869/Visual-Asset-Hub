using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VAH.Backend.Data;
using VAH.Backend.Models;

namespace VAH.Backend.Controllers;

/// <summary>
/// Server-side search endpoint.
/// Replaces client-side .includes() filtering for better performance and scalability.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly AppDbContext _context;

    public SearchController(AppDbContext context)
    {
        _context = context;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User identity not found.");

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
        pageSize = Math.Min(pageSize, 100);
        page = Math.Max(page, 1);
        var userId = GetUserId();

        var term = q?.Trim().ToLower() ?? string.Empty;

        // ── Search Assets (user-scoped) ──
        var assetQuery = _context.Assets
            .Where(a => a.UserId == userId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(term))
        {
            assetQuery = assetQuery.Where(a =>
                a.FileName.ToLower().Contains(term) ||
                a.Tags.ToLower().Contains(term));
        }

        if (!string.IsNullOrEmpty(type))
            assetQuery = assetQuery.Where(a => a.ContentType == type);

        if (collectionId.HasValue)
            assetQuery = assetQuery.Where(a => a.CollectionId == collectionId.Value);

        var totalAssets = await assetQuery.CountAsync();
        var assets = await assetQuery
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // ── Search Collections (user-scoped: system + own) ──
        var collections = new List<Collection>();
        var totalCollections = 0;

        if (string.IsNullOrEmpty(type) && !string.IsNullOrEmpty(term))
        {
            var collQuery = _context.Collections
                .Where(c => (c.UserId == userId || c.UserId == null) &&
                    (c.Name.ToLower().Contains(term) ||
                     c.Description.ToLower().Contains(term)));

            totalCollections = await collQuery.CountAsync();
            collections = await collQuery
                .OrderBy(c => c.Order)
                .Take(10) // Show max 10 matching collections
                .ToListAsync();
        }

        return Ok(new SearchResult
        {
            Query = q ?? string.Empty,
            Assets = assets,
            TotalAssets = totalAssets,
            Collections = collections,
            TotalCollections = totalCollections,
            Page = page,
            PageSize = pageSize,
            HasNextPage = page * pageSize < totalAssets,
        });
    }
}

/// <summary>
/// Combined search result for assets and collections.
/// </summary>
public class SearchResult
{
    public string Query { get; set; } = string.Empty;
    public List<Asset> Assets { get; set; } = new();
    public int TotalAssets { get; set; }
    public List<Collection> Collections { get; set; } = new();
    public int TotalCollections { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasNextPage { get; set; }
}
