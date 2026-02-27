using Microsoft.EntityFrameworkCore;
using VAH.Backend.Data;
using VAH.Backend.Models;

namespace VAH.Backend.Services;

public class SmartCollectionService : ISmartCollectionService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SmartCollectionService> _logger;

    public SmartCollectionService(AppDbContext context, ILogger<SmartCollectionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<SmartCollectionDefinition>> GetDefinitionsAsync(string userId)
    {
        var definitions = new List<SmartCollectionDefinition>();

        // 1. Recent uploads (last 7 days)
        var recentCount = await _context.Assets
            .CountAsync(a => a.UserId == userId && a.CreatedAt >= DateTime.UtcNow.AddDays(-7));
        definitions.Add(new SmartCollectionDefinition
        {
            Id = "recent-7d",
            Name = "Gần đây (7 ngày)",
            Description = "Các tài nguyên được tải lên trong 7 ngày qua",
            Icon = "🕐",
            Color = "#2196F3",
            Count = recentCount
        });

        // 2. Recent uploads (last 30 days)
        var monthCount = await _context.Assets
            .CountAsync(a => a.UserId == userId && a.CreatedAt >= DateTime.UtcNow.AddDays(-30));
        definitions.Add(new SmartCollectionDefinition
        {
            Id = "recent-30d",
            Name = "Tháng này",
            Description = "Các tài nguyên được tải lên trong 30 ngày qua",
            Icon = "📅",
            Color = "#4CAF50",
            Count = monthCount
        });

        // 3. All images
        var imageCount = await _context.Assets
            .CountAsync(a => a.UserId == userId && a.ContentType == AssetContentType.Image);
        definitions.Add(new SmartCollectionDefinition
        {
            Id = "all-images",
            Name = "Tất cả hình ảnh",
            Description = "Mọi hình ảnh đã tải lên",
            Icon = "🖼️",
            Color = "#FF9800",
            Count = imageCount
        });

        // 4. All links
        var linkCount = await _context.Assets
            .CountAsync(a => a.UserId == userId && a.ContentType == AssetContentType.Link);
        definitions.Add(new SmartCollectionDefinition
        {
            Id = "all-links",
            Name = "Tất cả liên kết",
            Description = "Mọi liên kết đã lưu",
            Icon = "🔗",
            Color = "#9C27B0",
            Count = linkCount
        });

        // 5. All colors
        var colorCount = await _context.Assets
            .CountAsync(a => a.UserId == userId && a.ContentType == AssetContentType.Color);
        definitions.Add(new SmartCollectionDefinition
        {
            Id = "all-colors",
            Name = "Tất cả màu sắc",
            Description = "Mọi mẫu màu đã lưu",
            Icon = "🎨",
            Color = "#E91E63",
            Count = colorCount
        });

        // 6. Untagged assets
        var untaggedCount = await _context.Assets
            .CountAsync(a => a.UserId == userId && !a.IsFolder &&
                !_context.AssetTags.Any(at => at.AssetId == a.Id));
        definitions.Add(new SmartCollectionDefinition
        {
            Id = "untagged",
            Name = "Chưa gắn tag",
            Description = "Tài nguyên chưa được gắn tag nào",
            Icon = "🏷️",
            Color = "#607D8B",
            Count = untaggedCount
        });

        // 7. Large files (files that have thumbnails — images)
        var largeCount = await _context.Assets
            .CountAsync(a => a.UserId == userId && a.ContentType == AssetContentType.Image && a.ThumbnailLg != null);
        definitions.Add(new SmartCollectionDefinition
        {
            Id = "with-thumbnails",
            Name = "Có thumbnail",
            Description = "Hình ảnh đã được tạo thumbnail",
            Icon = "📐",
            Color = "#00BCD4",
            Count = largeCount
        });

        // 8. Per-tag smart collections (top 10 most used tags)
        var topTags = await _context.Tags
            .Where(t => t.UserId == userId)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Color,
                Count = _context.AssetTags.Count(at => at.TagId == t.Id)
            })
            .OrderByDescending(t => t.Count)
            .Take(10)
            .ToListAsync();

        foreach (var tag in topTags.Where(t => t.Count > 0))
        {
            definitions.Add(new SmartCollectionDefinition
            {
                Id = $"tag-{tag.Id}",
                Name = $"Tag: {tag.Name}",
                Description = $"Tài nguyên được gắn tag '{tag.Name}'",
                Icon = "🏷️",
                Color = tag.Color ?? "#78909C",
                Count = tag.Count
            });
        }

        return definitions;
    }

    public async Task<PagedResult<Asset>> GetItemsAsync(string smartCollectionId, PaginationParams pagination, string userId)
    {
        IQueryable<Asset> query = _context.Assets
            .Where(a => a.UserId == userId && !a.IsFolder);

        // Apply smart collection filter
        query = smartCollectionId switch
        {
            "recent-7d" => query.Where(a => a.CreatedAt >= DateTime.UtcNow.AddDays(-7)),
            "recent-30d" => query.Where(a => a.CreatedAt >= DateTime.UtcNow.AddDays(-30)),
            "all-images" => query.Where(a => a.ContentType == AssetContentType.Image),
            "all-links" => query.Where(a => a.ContentType == AssetContentType.Link),
            "all-colors" => query.Where(a => a.ContentType == AssetContentType.Color),
            "untagged" => query.Where(a => !_context.AssetTags.Any(at => at.AssetId == a.Id)),
            "with-thumbnails" => query.Where(a => a.ContentType == AssetContentType.Image && a.ThumbnailLg != null),
            _ when smartCollectionId.StartsWith("tag-") => ApplyTagFilter(query, smartCollectionId),
            _ => query
        };

        // Sorting
        query = pagination.SortBy?.ToLower() switch
        {
            "filename" => pagination.SortOrder == "desc"
                ? query.OrderByDescending(a => a.FileName)
                : query.OrderBy(a => a.FileName),
            _ => query.OrderByDescending(a => a.CreatedAt) // default
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return new PagedResult<Asset>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    private IQueryable<Asset> ApplyTagFilter(IQueryable<Asset> query, string smartCollectionId)
    {
        if (int.TryParse(smartCollectionId.Replace("tag-", ""), out var tagId))
        {
            var assetIdsWithTag = _context.AssetTags
                .Where(at => at.TagId == tagId)
                .Select(at => at.AssetId);
            query = query.Where(a => assetIdsWithTag.Contains(a.Id));
        }
        return query;
    }
}
