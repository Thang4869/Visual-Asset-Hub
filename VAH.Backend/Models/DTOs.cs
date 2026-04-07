using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace VAH.Backend.Models;

// ──── Response DTOs (Clean Architecture — never leak domain entities) ────

/// <summary>
/// API response DTO for Asset. Prevents domain model leakage across API boundary.
/// </summary>
public class AssetResponseDto
{
    public int Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public string Tags { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public double PositionX { get; init; }
    public double PositionY { get; init; }
    public int CollectionId { get; init; }
    public AssetContentType ContentType { get; init; }
    public int? GroupId { get; init; }
    public int? ParentFolderId { get; init; }
    public int SortOrder { get; init; }
    public bool IsFolder { get; init; }
    public string? ThumbnailSm { get; init; }
    public string? ThumbnailMd { get; init; }
    public string? ThumbnailLg { get; init; }
}

// ──── Asset Creation DTOs ────

public class CreateAssetDto
{
    [Required, MaxLength(500)]
    public string FileName { get; init; } = string.Empty;

    [Required, MaxLength(2048)]
    public string FilePath { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CollectionId { get; init; } = 1;

    public int? ParentFolderId { get; init; }
}

public class CreateFolderDto
{
    [Required, MaxLength(255)]
    public string FolderName { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CollectionId { get; init; } = 1;

    public int? ParentFolderId { get; init; }
}

public class CreateColorDto
{
    [Required, MaxLength(50)]
    public string ColorCode { get; init; } = string.Empty;

    [MaxLength(100)]
    public string? ColorName { get; init; }

    [Range(1, int.MaxValue)]
    public int CollectionId { get; init; } = 1;

    public int? GroupId { get; init; }
    public int? SortOrder { get; init; }
    public int? ParentFolderId { get; init; }
}

public class UpdateAssetDto
{
    [MaxLength(500)]
    public string? FileName { get; init; }

    public int? SortOrder { get; init; }
    public int? GroupId { get; init; }
    public int? ParentFolderId { get; init; }
    public bool? ClearParentFolder { get; init; }
    public bool? ClearGroup { get; init; }
}

public class CreateLinkDto
{
    [Required, MaxLength(500)]
    public string Name { get; init; } = string.Empty;

    [Required, MaxLength(2048)]
    public string Url { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CollectionId { get; init; } = 1;

    public int? ParentFolderId { get; init; }
}

public class CreateColorGroupDto
{
    [Required, MaxLength(255)]
    public string GroupName { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CollectionId { get; init; } = 1;

    public int? ParentFolderId { get; init; }
    public int? SortOrder { get; init; }
}

public class ReorderAssetsDto
{
    [Required]
    public List<int> AssetIds { get; init; } = new List<int>();
}


/// <summary>Typed response for bulk delete operations.</summary>
public sealed record BulkDeleteResult(int Deleted);

/// <summary>Typed response for bulk move operations.</summary>
public sealed record BulkMoveResult(int Moved);

/// <summary>Typed response for bulk tag operations.</summary>
public sealed record BulkTagResult(int Affected);

// ──── Bulk Operation DTOs ────

public class BulkDeleteDto
{
    [Required]
    public List<int> AssetIds { get; init; } = new();
}

public class BulkMoveDto
{
    [Required]
    public List<int> AssetIds { get; init; } = new();

    public int? TargetCollectionId { get; init; }
    public int? TargetFolderId { get; init; }
    public bool? ClearParentFolder { get; init; }
}

public class BulkMoveGroupDto
{
    [Required]
    public List<int> AssetIds { get; init; } = new();

    /// <summary>Target group ID. Null means "Ungrouped".</summary>
    public int? TargetGroupId { get; init; }

    /// <summary>Insert before this asset ID within the group. Null = append at end.</summary>
    public int? InsertBeforeId { get; init; }
}

public class BulkTagDto
{
    [Required]
    public List<int> AssetIds { get; init; } = new();

    [Required]
    public List<int> TagIds { get; init; } = new();

    /// <summary>If true, removes these tags. If false (default), adds them.</summary>
    public bool Remove { get; init; } = false;
}

// ──── Collection Creation DTO ────

public class CreateCollectionDto
{
    [Required, MaxLength(255)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; init; }

    public int? ParentId { get; init; }

    [MaxLength(20)]
    public string? Color { get; init; }

    public CollectionType? Type { get; init; }
    public LayoutType? LayoutType { get; init; }
}

// ──── Collection Update DTO ────

public class UpdateCollectionDto
{
    [MaxLength(255)]
    public string? Name { get; init; }

    [MaxLength(2000)]
    public string? Description { get; init; }

    [MaxLength(20)]
    public string? Color { get; init; }

    public CollectionType? Type { get; init; }
    public int? Order { get; init; }
    public LayoutType? LayoutType { get; init; }
}

// ──── Position DTOs ────

public class AssetPositionDto
{
    [Range(-1_000_000, 1_000_000)]
    public double PositionX { get; init; }

    [Range(-1_000_000, 1_000_000)]
    public double PositionY { get; init; }
}

// ──── Result DTOs ────

/// <summary>
/// Combined search result for assets and collections.
/// </summary>
public class SearchResult
{
    public string Query { get; init; } = string.Empty;
    public List<AssetResponseDto> Assets { get; init; } = new();
    public int TotalAssets { get; init; }
    public List<Collection> Collections { get; init; } = new();
    public int TotalCollections { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public bool HasNextPage { get; init; }
}

/// <summary>
/// Result object for collection with items query.
/// </summary>
public class CollectionWithItemsResult
{
    public Collection Collection { get; init; } = null!;
    public List<AssetResponseDto> Items { get; init; } = new();
    public List<Collection> SubCollections { get; init; } = new();
}

/// <summary>
/// Describes a smart (dynamic) collection.
/// </summary>
public class SmartCollectionDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Icon { get; init; } = "📁";
    public string Color { get; init; } = "#2196F3";
    public int Count { get; init; }
}

// ──── Thin response DTOs (replaces anonymous objects for type-safe Swagger) ────

/// <summary>Typed response for role queries — replaces <c>new { role }</c>.</summary>
public sealed record RoleResult(string? Role);

/// <summary>Typed response for one-off operational messages.</summary>
public sealed record MessageResult(string Message);

// ──── Search request DTO (cohesion: groups all search params) ────

/// <summary>
/// Query-string parameters for the search endpoint.
/// Grouping avoids primitive-obsession and enables validation in one place.
/// </summary>
public sealed class SearchRequestParams
{
    [FromQuery(Name = "q")]
    public string? Query { get; init; }

    [FromQuery(Name = "type")]
    public string? Type { get; init; }

    [FromQuery(Name = "collectionId")]
    public int? CollectionId { get; init; }

    [FromQuery(Name = "page")]
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [FromQuery(Name = "pageSize")]
    [Range(1, 200)]
    public int PageSize { get; init; } = 50;
}

