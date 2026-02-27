using System.ComponentModel.DataAnnotations;

namespace VAH.Backend.Models;

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

// ──── Tag DTOs ────

public class CreateTagDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Color { get; set; }
}

public class UpdateTagDto
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(20)]
    public string? Color { get; set; }
}

public class AssetTagsDto
{
    [Required]
    public List<int> TagIds { get; set; } = new();
}

// ──── Bulk Operation DTOs ────

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

public class BulkTagDto
{
    [Required]
    public List<int> AssetIds { get; set; } = new();

    [Required]
    public List<int> TagIds { get; set; } = new();

    /// <summary>If true, removes these tags. If false (default), adds them.</summary>
    public bool Remove { get; set; } = false;
}

// ──── Position DTOs ────

public class AssetPositionDto
{
    public double PositionX { get; set; }
    public double PositionY { get; set; }
}

// ──── Result DTOs ────

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

/// <summary>
/// Result object for collection with items query.
/// </summary>
public class CollectionWithItemsResult
{
    public Collection Collection { get; set; } = null!;
    public List<Asset> Items { get; set; } = new();
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
    public string Icon { get; set; } = "📁";
    public string Color { get; set; } = "#2196F3";
    public int Count { get; set; }
}
