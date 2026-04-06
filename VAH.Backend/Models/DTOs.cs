using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace VAH.Backend.Models;

// â”€â”€â”€â”€ Response DTOs (Clean Architecture â€” never leak domain entities) â”€â”€â”€â”€

/// <summary>
/// API response DTO for Asset. Prevents domain model leakage across API boundary.
/// </summary>
public class AssetResponseDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public int CollectionId { get; set; }
    public AssetContentType ContentType { get; set; }
    public int? GroupId { get; set; }
    public int? ParentFolderId { get; set; }
    public int SortOrder { get; set; }
    public bool IsFolder { get; set; }
    public string? ThumbnailSm { get; set; }
    public string? ThumbnailMd { get; set; }
    public string? ThumbnailLg { get; set; }
}

// â”€â”€â”€â”€ Asset Creation DTOs â”€â”€â”€â”€

public class CreateAssetDto
{
    [Required, MaxLength(500)]
    public string FileName { get; set; } = string.Empty;

    [Required, MaxLength(2048)]
    public string FilePath { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CollectionId { get; set; } = 1;

    public int? ParentFolderId { get; set; }
}

public class CreateFolderDto
{
    [Required, MaxLength(255)]
    public string FolderName { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CollectionId { get; set; } = 1;

    public int? ParentFolderId { get; set; }
}

public class CreateColorDto
{
    [Required, MaxLength(50)]
    public string ColorCode { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ColorName { get; set; }

    [Range(1, int.MaxValue)]
    public int CollectionId { get; set; } = 1;

    public int? GroupId { get; set; }
    public int? SortOrder { get; set; }
    public int? ParentFolderId { get; set; }
}

public class UpdateAssetDto
{
    [MaxLength(500)]
    public string? FileName { get; set; }

    public int? SortOrder { get; set; }
    public int? GroupId { get; set; }
    public int? ParentFolderId { get; set; }
    public bool? ClearParentFolder { get; set; }
    public bool? ClearGroup { get; set; }
}

public class CreateLinkDto
{
    [Required, MaxLength(500)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(2048)]
    public string Url { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CollectionId { get; set; } = 1;

    public int? ParentFolderId { get; set; }
}

public class CreateColorGroupDto
{
    [Required, MaxLength(255)]
    public string GroupName { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CollectionId { get; set; } = 1;

    public int? ParentFolderId { get; set; }
    public int? SortOrder { get; set; }
}

public class ReorderAssetsDto
{
    [Required]
    public List<int> AssetIds { get; set; } = new List<int>();
}


/// <summary>Typed response for bulk delete operations.</summary>
public sealed record BulkDeleteResult(int Deleted);

/// <summary>Typed response for bulk move operations.</summary>
public sealed record BulkMoveResult(int Moved);

/// <summary>Typed response for bulk tag operations.</summary>
public sealed record BulkTagResult(int Affected);

// â”€â”€â”€â”€ Bulk Operation DTOs â”€â”€â”€â”€

public class BulkDeleteDto
{
    [Required]
    public List<int> AssetIds { get; set; } = new();
}

public class BulkMoveDto
{
    [Required]
    public List<int> AssetIds { get; set; } = new();

    public int? TargetCollectionId { get; set; }
    public int? TargetFolderId { get; set; }
    public bool? ClearParentFolder { get; set; }
}

public class BulkMoveGroupDto
{
    [Required]
    public List<int> AssetIds { get; set; } = new();

    /// <summary>Target group ID. Null means "Ungrouped".</summary>
    public int? TargetGroupId { get; set; }

    /// <summary>Insert before this asset ID within the group. Null = append at end.</summary>
    public int? InsertBeforeId { get; set; }
}

public class BulkTagDto
{
    [Required]
    public List<int> AssetIds { get; set; } = new();

    [Required]
    public List<int> TagIds { get; set; } = new();

    /// <summary>If true, removes these tags. If false (default), adds them.</summary>
    public bool Remove { get; set; } = false;
}

// â”€â”€â”€â”€ Collection Creation DTO â”€â”€â”€â”€

public class CreateCollectionDto
{
    [Required, MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public int? ParentId { get; set; }

    [MaxLength(20)]
    public string? Color { get; set; }

    public CollectionType? Type { get; set; }
    public LayoutType? LayoutType { get; set; }
}

// â”€â”€â”€â”€ Collection Update DTO â”€â”€â”€â”€

public class UpdateCollectionDto
{
    [MaxLength(255)]
    public string? Name { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(20)]
    public string? Color { get; set; }

    public CollectionType? Type { get; set; }
    public int? Order { get; set; }
    public LayoutType? LayoutType { get; set; }
}

// â”€â”€â”€â”€ Position DTOs â”€â”€â”€â”€

public class AssetPositionDto
{
    [Range(-1_000_000, 1_000_000)]
    public double PositionX { get; set; }

    [Range(-1_000_000, 1_000_000)]
    public double PositionY { get; set; }
}

// â”€â”€â”€â”€ Result DTOs â”€â”€â”€â”€

/// <summary>
/// Combined search result for assets and collections.
/// </summary>
public class SearchResult
{
    public string Query { get; set; } = string.Empty;
    public List<AssetResponseDto> Assets { get; set; } = new();
    public int TotalAssets { get; set; }
    public List<Collection> Collections { get; set; } = new();
    public int TotalCollections { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasNextPage { get; set; }
}

/// <summary>
/// Result object for collection with items query.
/// </summary>
public class CollectionWithItemsResult
{
    public Collection Collection { get; set; } = null!;
    public List<AssetResponseDto> Items { get; set; } = new();
    public List<Collection> SubCollections { get; set; } = new();
}

/// <summary>
/// Describes a smart (dynamic) collection.
/// </summary>
public class SmartCollectionDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "đŸ“";
    public string Color { get; set; } = "#2196F3";
    public int Count { get; set; }
}

// â”€â”€â”€â”€ Thin response DTOs (replaces anonymous objects for type-safe Swagger) â”€â”€â”€â”€

/// <summary>Typed response for role queries â€” replaces <c>new { role }</c>.</summary>
public sealed record RoleResult(string? Role);

/// <summary>Typed response for one-off operational messages.</summary>
public sealed record MessageResult(string Message);

// â”€â”€â”€â”€ Search request DTO (cohesion: groups all search params) â”€â”€â”€â”€

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

