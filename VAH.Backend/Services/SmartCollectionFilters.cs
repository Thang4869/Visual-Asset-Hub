using VAH.Backend.Data;
using VAH.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace VAH.Backend.Services;

/// <summary>
/// Defines a single smart collection filter (Strategy pattern).
/// Each implementation knows how to describe itself and filter assets.
/// OOP: Open/Closed — add new smart collections without modifying existing code.
/// </summary>
public interface ISmartCollectionFilter
{
    /// <summary>Unique identifier for this smart collection (e.g. "recent-7d").</summary>
    string Id { get; }

    /// <summary>Build the definition (name, icon, count) for a given user.</summary>
    Task<SmartCollectionDefinition> GetDefinitionAsync(AppDbContext context, string userId);

    /// <summary>Apply the filter predicate to an asset query.</summary>
    IQueryable<Asset> ApplyFilter(IQueryable<Asset> query, AppDbContext context);
}

// ─────────────────────────────────────────────
//  Concrete strategies
// ─────────────────────────────────────────────

/// <summary>Assets created within the last N days.</summary>
public class RecentDaysFilter : ISmartCollectionFilter
{
    private readonly int _days;
    private readonly string _id;
    private readonly string _name;
    private readonly string _description;
    private readonly string _icon;
    private readonly string _color;

    public RecentDaysFilter(int days, string id, string name, string description, string icon, string color)
    {
        _days = days;
        _id = id;
        _name = name;
        _description = description;
        _icon = icon;
        _color = color;
    }

    public string Id => _id;

    public async Task<SmartCollectionDefinition> GetDefinitionAsync(AppDbContext context, string userId)
    {
        var cutoff = DateTime.UtcNow.AddDays(-_days);
        var count = await context.Assets.CountAsync(a => a.UserId == userId && a.CreatedAt >= cutoff);
        return new SmartCollectionDefinition
        {
            Id = _id, Name = _name, Description = _description,
            Icon = _icon, Color = _color, Count = count
        };
    }

    public IQueryable<Asset> ApplyFilter(IQueryable<Asset> query, AppDbContext context)
    {
        var cutoff = DateTime.UtcNow.AddDays(-_days);
        return query.Where(a => a.CreatedAt >= cutoff);
    }
}

/// <summary>Assets of a specific content type.</summary>
public class ContentTypeFilter : ISmartCollectionFilter
{
    private readonly AssetContentType _contentType;
    private readonly string _id;
    private readonly string _name;
    private readonly string _description;
    private readonly string _icon;
    private readonly string _color;

    public ContentTypeFilter(AssetContentType contentType, string id, string name,
        string description, string icon, string color)
    {
        _contentType = contentType;
        _id = id;
        _name = name;
        _description = description;
        _icon = icon;
        _color = color;
    }

    public string Id => _id;

    public async Task<SmartCollectionDefinition> GetDefinitionAsync(AppDbContext context, string userId)
    {
        var count = await context.Assets.CountAsync(a => a.UserId == userId && a.ContentType == _contentType);
        return new SmartCollectionDefinition
        {
            Id = _id, Name = _name, Description = _description,
            Icon = _icon, Color = _color, Count = count
        };
    }

    public IQueryable<Asset> ApplyFilter(IQueryable<Asset> query, AppDbContext context)
    {
        return query.Where(a => a.ContentType == _contentType);
    }
}

/// <summary>Assets that have no tags.</summary>
public class UntaggedFilter : ISmartCollectionFilter
{
    public string Id => "untagged";

    public async Task<SmartCollectionDefinition> GetDefinitionAsync(AppDbContext context, string userId)
    {
        var count = await context.Assets.CountAsync(a =>
            a.UserId == userId && !a.IsFolder &&
            !context.AssetTags.Any(at => at.AssetId == a.Id));

        return new SmartCollectionDefinition
        {
            Id = Id, Name = "Chưa gắn tag",
            Description = "Tài nguyên chưa được gắn tag nào",
            Icon = "🏷️", Color = "#607D8B", Count = count
        };
    }

    public IQueryable<Asset> ApplyFilter(IQueryable<Asset> query, AppDbContext context)
    {
        return query.Where(a => !context.AssetTags.Any(at => at.AssetId == a.Id));
    }
}

/// <summary>Image assets that have generated thumbnails.</summary>
public class WithThumbnailsFilter : ISmartCollectionFilter
{
    public string Id => "with-thumbnails";

    public async Task<SmartCollectionDefinition> GetDefinitionAsync(AppDbContext context, string userId)
    {
        var count = await context.Assets.CountAsync(a =>
            a.UserId == userId && a.ContentType == AssetContentType.Image && a.ThumbnailLg != null);

        return new SmartCollectionDefinition
        {
            Id = Id, Name = "Có thumbnail",
            Description = "Hình ảnh đã được tạo thumbnail",
            Icon = "📐", Color = "#00BCD4", Count = count
        };
    }

    public IQueryable<Asset> ApplyFilter(IQueryable<Asset> query, AppDbContext context)
    {
        return query.Where(a => a.ContentType == AssetContentType.Image && a.ThumbnailLg != null);
    }
}

/// <summary>Assets tagged with a specific tag (dynamic — created per tag).</summary>
public class TagFilter : ISmartCollectionFilter
{
    private readonly int _tagId;
    private readonly string _tagName;
    private readonly string? _tagColor;

    public TagFilter(int tagId, string tagName, string? tagColor)
    {
        _tagId = tagId;
        _tagName = tagName;
        _tagColor = tagColor;
    }

    public string Id => $"tag-{_tagId}";

    public async Task<SmartCollectionDefinition> GetDefinitionAsync(AppDbContext context, string userId)
    {
        var count = await context.AssetTags.CountAsync(at => at.TagId == _tagId);
        return new SmartCollectionDefinition
        {
            Id = Id, Name = $"Tag: {_tagName}",
            Description = $"Tài nguyên được gắn tag '{_tagName}'",
            Icon = "🏷️", Color = _tagColor ?? "#78909C", Count = count
        };
    }

    public IQueryable<Asset> ApplyFilter(IQueryable<Asset> query, AppDbContext context)
    {
        var assetIdsWithTag = context.AssetTags
            .Where(at => at.TagId == _tagId)
            .Select(at => at.AssetId);
        return query.Where(a => assetIdsWithTag.Contains(a.Id));
    }
}
