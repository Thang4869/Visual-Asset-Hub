using Microsoft.EntityFrameworkCore;
using VAH.Backend.Data;
using VAH.Backend.Models;

namespace VAH.Backend.Services;

/// <summary>
/// SmartCollectionService — uses Strategy pattern via ISmartCollectionFilter registry.
/// OOP: Open/Closed — add new smart collections by adding an ISmartCollectionFilter implementation,
/// without modifying this service.
/// </summary>
public class SmartCollectionService : ISmartCollectionService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SmartCollectionService> _logger;

    /// <summary>Static (built-in) filters — registered once.</summary>
    private static readonly List<ISmartCollectionFilter> BuiltInFilters = new()
    {
        new RecentDaysFilter(7, "recent-7d", "Gần đây (7 ngày)",
            "Các tài nguyên được tải lên trong 7 ngày qua", "🕐", "#2196F3"),
        new RecentDaysFilter(30, "recent-30d", "Tháng này",
            "Các tài nguyên được tải lên trong 30 ngày qua", "📅", "#4CAF50"),
        new ContentTypeFilter(AssetContentType.Image, "all-images", "Tất cả hình ảnh",
            "Mọi hình ảnh đã tải lên", "🖼️", "#FF9800"),
        new ContentTypeFilter(AssetContentType.Link, "all-links", "Tất cả liên kết",
            "Mọi liên kết đã lưu", "🔗", "#9C27B0"),
        new ContentTypeFilter(AssetContentType.Color, "all-colors", "Tất cả màu sắc",
            "Mọi mẫu màu đã lưu", "🎨", "#E91E63"),
        new UntaggedFilter(),
        new WithThumbnailsFilter(),
    };

    public SmartCollectionService(AppDbContext context, ILogger<SmartCollectionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<SmartCollectionDefinition>> GetDefinitionsAsync(string userId, CancellationToken ct = default)
    {
        var definitions = new List<SmartCollectionDefinition>();

        // 1. Built-in filters
        foreach (var filter in BuiltInFilters)
        {
            ct.ThrowIfCancellationRequested();
            definitions.Add(await filter.GetDefinitionAsync(_context, userId));
        }

        // 2. Dynamic tag-based filters (top 10 most used tags)
        var tagFilters = await BuildTagFiltersAsync(userId, ct);
        foreach (var filter in tagFilters)
        {
            ct.ThrowIfCancellationRequested();
            definitions.Add(await filter.GetDefinitionAsync(_context, userId));
        }

        return definitions;
    }

    public async Task<PagedResult<AssetResponseDto>> GetItemsAsync(string smartCollectionId, PaginationParams pagination, string userId, CancellationToken ct = default)
    {
        IQueryable<Asset> query = _context.Assets
            .Where(a => a.UserId == userId && !a.IsFolder);

        // Find matching filter from registry
        var filter = FindFilter(smartCollectionId)
                     ?? await FindDynamicFilterAsync(smartCollectionId, userId, ct);

        if (filter != null)
        {
            query = filter.ApplyFilter(query, _context);
        }

        // Sorting
        query = pagination.SortBy?.ToLower() switch
        {
            "filename" => pagination.SortOrder == "desc"
                ? query.OrderByDescending(a => a.FileName)
                : query.OrderBy(a => a.FileName),
            _ => query.OrderByDescending(a => a.CreatedAt)
        };

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(ct);

        return new PagedResult<AssetResponseDto>
        {
            Items = items.Select(a => a.ToDto()).ToList(),
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    /// <summary>Look up a built-in filter by ID.</summary>
    private static ISmartCollectionFilter? FindFilter(string id)
    {
        return BuiltInFilters.FirstOrDefault(f => f.Id == id);
    }

    /// <summary>Build a dynamic TagFilter if the ID matches the tag-{id} pattern.</summary>
    private async Task<ISmartCollectionFilter?> FindDynamicFilterAsync(string smartCollectionId, string userId, CancellationToken ct)
    {
        if (!smartCollectionId.StartsWith("tag-")) return null;

        if (!int.TryParse(smartCollectionId.Replace("tag-", ""), out var tagId)) return null;

        var tag = await _context.Tags
            .Where(t => t.Id == tagId && t.UserId == userId)
            .Select(t => new { t.Id, t.Name, t.Color })
            .FirstOrDefaultAsync(ct);

        return tag != null ? new TagFilter(tag.Id, tag.Name, tag.Color) : null;
    }

    /// <summary>Build TagFilter strategies for the user's top-10 most-used tags.</summary>
    private async Task<List<TagFilter>> BuildTagFiltersAsync(string userId, CancellationToken ct)
    {
        var topTags = await _context.Tags
            .Where(t => t.UserId == userId)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Color,
                Count = _context.AssetTags.Count(at => at.TagId == t.Id)
            })
            .Where(t => t.Count > 0)
            .OrderByDescending(t => t.Count)
            .Take(10)
            .ToListAsync(ct);

        return topTags.Select(t => new TagFilter(t.Id, t.Name, t.Color)).ToList();
    }
}
