using Microsoft.EntityFrameworkCore;
using VAH.Backend.Data;
using VAH.Backend.Models;

namespace VAH.Backend.Services;

/// <summary>
/// Search service implementation — queries assets and collections by name/tags.
/// </summary>
public class SearchService : ISearchService
{
    private readonly AppDbContext _context;

    public SearchService(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<SearchResult> SearchAsync(string userId, string? query, string? type, int? collectionId, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        pageSize = Math.Min(pageSize, 100);
        page = Math.Max(page, 1);

        var term = query?.Trim().ToLower() ?? string.Empty;

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
        {
            var contentTypeEnum = type.ToAssetContentType();
            assetQuery = assetQuery.Where(a => a.ContentType == contentTypeEnum);
        }

        if (collectionId.HasValue)
            assetQuery = assetQuery.Where(a => a.CollectionId == collectionId.Value);

        var totalAssets = await assetQuery.CountAsync(ct);
        var assets = await assetQuery
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        // ── Search Collections (user-scoped: system + own) ──
        var collections = new List<Collection>();
        var totalCollections = 0;

        if (string.IsNullOrEmpty(type) && !string.IsNullOrEmpty(term))
        {
            var collQuery = _context.Collections
                .Where(c => (c.UserId == userId || c.UserId == null) &&
                    (c.Name.ToLower().Contains(term) ||
                     c.Description.ToLower().Contains(term)));

            totalCollections = await collQuery.CountAsync(ct);
            collections = await collQuery
                .OrderBy(c => c.Order)
                .Take(10) // Show max 10 matching collections
                .ToListAsync(ct);
        }

        return new SearchResult
        {
            Query = query ?? string.Empty,
            Assets = assets.Select(a => a.ToDto()).ToList(),
            TotalAssets = totalAssets,
            Collections = collections,
            TotalCollections = totalCollections,
            Page = page,
            PageSize = pageSize,
            HasNextPage = page * pageSize < totalAssets,
        };
    }
}
