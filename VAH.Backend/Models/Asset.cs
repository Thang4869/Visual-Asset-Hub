using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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

    /// <summary>Parent collection this asset belongs to.</summary>
    [JsonIgnore]
    public Collection? Collection { get; set; }

    /// <summary>Parent folder (self-referencing via ParentFolderId).</summary>
    [JsonIgnore]
    public Asset? ParentFolder { get; set; }

    // ── Virtual behavior properties (overridden by TPH subtypes) ──

    /// <summary>Whether this asset type has a physical file stored on disk.</summary>
    public virtual bool HasPhysicalFile => true;

    /// <summary>Whether thumbnails can/should be generated for this asset type.</summary>
    public virtual bool CanHaveThumbnails => false;

    /// <summary>Whether physical file cleanup is needed on delete.</summary>
    public virtual bool RequiresFileCleanup =>
        HasPhysicalFile && !string.IsNullOrEmpty(FilePath) && FilePath.StartsWith("/uploads/");

    // ── Domain behavior methods ──

    /// <summary>Update canvas position coordinates.</summary>
    public void UpdatePosition(double x, double y)
    {
        PositionX = x;
        PositionY = y;
    }

    /// <summary>Apply partial update from DTO. Only non-null fields are applied.</summary>
    public void ApplyUpdate(UpdateAssetDto dto)
    {
        if (!string.IsNullOrEmpty(dto.FileName))
            FileName = dto.FileName.Trim();
        if (dto.SortOrder.HasValue)
            SortOrder = dto.SortOrder.Value;
        if (dto.ClearGroup == true)
            GroupId = null;
        else if (dto.GroupId.HasValue)
            GroupId = dto.GroupId.Value;
        if (dto.ParentFolderId.HasValue)
            ParentFolderId = dto.ParentFolderId.Value;
        if (dto.ClearParentFolder == true)
            ParentFolderId = null;
    }

    /// <summary>Set generated thumbnail paths.</summary>
    public void SetThumbnails(string? sm, string? md, string? lg)
    {
        ThumbnailSm = sm;
        ThumbnailMd = md;
        ThumbnailLg = lg;
    }

    /// <summary>Move asset to a different folder (or root if null).</summary>
    public void MoveToFolder(int? folderId) => ParentFolderId = folderId;

    /// <summary>Move asset to a different collection.</summary>
    public void MoveToCollection(int collectionId) => CollectionId = collectionId;

    /// <summary>Check if this asset is owned by a specific user.</summary>
    public bool IsOwnedBy(string userId) => UserId == userId;

    /// <summary>Whether this is a system/shared asset (no owner).</summary>
    public bool IsSystemAsset => UserId == null;
}