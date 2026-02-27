using System.ComponentModel.DataAnnotations;

namespace VAH.Backend.Models;

/// <summary>
/// Base class for all digital assets in the system (TPH base).
/// Default discriminator value: File. Subtypes: ImageAsset, LinkAsset,
/// ColorAsset, ColorGroupAsset, FolderAsset.
/// </summary>
public class Asset
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(500)]
    public string FileName { get; set; } = string.Empty;

    [Required, MaxLength(2048)]
    public string FilePath { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Tags { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public double PositionX { get; set; } = 0;
    public double PositionY { get; set; } = 0;

    public int CollectionId { get; set; } = 1;

    public AssetContentType ContentType { get; set; } = AssetContentType.File;

    public int? GroupId { get; set; } = null;
    public int? ParentFolderId { get; set; } = null;
    public int SortOrder { get; set; } = 0;
    public bool IsFolder { get; set; } = false;

    /// <summary>
    /// Owner of this asset. Null for system/shared assets.
    /// </summary>
    public string? UserId { get; set; }

    // --- Thumbnails (generated server-side for images) ---

    /// <summary>Small thumbnail (150px max), WebP. Null if not an image or not yet generated.</summary>
    [MaxLength(2048)]
    public string? ThumbnailSm { get; set; }

    /// <summary>Medium thumbnail (400px max), WebP.</summary>
    [MaxLength(2048)]
    public string? ThumbnailMd { get; set; }

    /// <summary>Large thumbnail (800px max), WebP.</summary>
    [MaxLength(2048)]
    public string? ThumbnailLg { get; set; }

    // --- Navigation properties ---
    public ICollection<AssetTag> AssetTags { get; set; } = new List<AssetTag>();

    // ── Virtual behavior properties (overridden by TPH subtypes) ──

    /// <summary>Whether this asset type has a physical file stored on disk.</summary>
    public virtual bool HasPhysicalFile => true;

    /// <summary>Whether thumbnails can/should be generated for this asset type.</summary>
    public virtual bool CanHaveThumbnails => false;

    /// <summary>Whether physical file cleanup is needed on delete.</summary>
    public virtual bool RequiresFileCleanup =>
        HasPhysicalFile && !string.IsNullOrEmpty(FilePath) && FilePath.StartsWith("/uploads/");
}