using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VAH.Backend.Models;

/// <summary>
/// Abstract base for all digital assets (TPH root).
/// Never instantiate directly — use a concrete subtype via AssetFactory.
/// All state mutations go through domain methods; setters are private.
/// Subtypes: FileAsset, ImageAsset, LinkAsset, ColorAsset, ColorGroupAsset, FolderAsset.
/// </summary>
public abstract class Asset
{
    // ── Constructors ──

    /// <summary>EF Core materialization (reflection can set private setters).</summary>
    protected Asset() { }

    /// <summary>Domain constructor — called by subclass constructors via base(...).</summary>
    protected Asset(
        string fileName, string filePath, AssetContentType contentType,
        int collectionId, string userId,
        int? parentFolderId = null, int? groupId = null,
        int sortOrder = 0, bool isFolder = false)
    {
        FileName = fileName;
        FilePath = filePath;
        ContentType = contentType;
        CollectionId = collectionId;
        UserId = userId;
        ParentFolderId = parentFolderId;
        GroupId = groupId;
        SortOrder = sortOrder;
        IsFolder = isFolder;
        CreatedAt = DateTime.UtcNow;
    }

    // ── Persisted properties (private set — mutation via domain methods only) ──

    [Key]
    public int Id { get; private set; }

    [Required, MaxLength(500)]
    public string FileName { get; private set; } = string.Empty;

    [Required, MaxLength(2048)]
    public string FilePath { get; private set; } = string.Empty;

    /// <summary>Legacy comma-separated tags. Use AssetTags navigation for new code.</summary>
    [Obsolete("Use AssetTags navigation property instead. Kept for backward-compatible search queries.")]
    [MaxLength(2000)]
    public string Tags { get; internal set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    // ── Audit ──

    public DateTime? UpdatedAt { get; private set; }

    /// <summary>Soft-delete flag. True = logically deleted.</summary>
    public bool IsDeleted { get; private set; }

    public double PositionX { get; private set; }
    public double PositionY { get; private set; }

    public int CollectionId { get; private set; }

    public AssetContentType ContentType { get; private set; } = AssetContentType.File;

    public int? GroupId { get; private set; }
    public int? ParentFolderId { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsFolder { get; private set; }

    /// <summary>Owner of this asset. Null for system/shared assets.</summary>
    public string? UserId { get; private set; }

    // --- Thumbnails (generated server-side for images) ---

    /// <summary>Small thumbnail (150px max), WebP.</summary>
    [MaxLength(2048)]
    public string? ThumbnailSm { get; private set; }

    /// <summary>Medium thumbnail (400px max), WebP.</summary>
    [MaxLength(2048)]
    public string? ThumbnailMd { get; private set; }

    /// <summary>Large thumbnail (800px max), WebP.</summary>
    [MaxLength(2048)]
    public string? ThumbnailLg { get; private set; }

    // --- Navigation properties (EF Core manages these) ---
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

    // ── Domain behavior methods (the ONLY way to mutate state) ──

    /// <summary>Update canvas position coordinates.</summary>
    public void UpdatePosition(double x, double y)
    {
        PositionX = x;
        PositionY = y;
    }

    /// <summary>Rename this asset.</summary>
    public void Rename(string newName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);
        FileName = newName.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Change the sort position of this asset.</summary>
    public void Reorder(int sortOrder)
    {
        SortOrder = sortOrder;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Assign this asset to a color group.</summary>
    public void AssignToGroup(int groupId)
    {
        GroupId = groupId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Remove this asset from its current color group.</summary>
    public void RemoveFromGroup()
    {
        GroupId = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Set generated thumbnail paths.</summary>
    public void SetThumbnails(string? sm, string? md, string? lg)
    {
        ThumbnailSm = sm;
        ThumbnailMd = md;
        ThumbnailLg = lg;
    }

    /// <summary>Move asset to a different folder (or root if null).</summary>
    public void MoveToFolder(int? folderId)
    {
        ParentFolderId = folderId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Move asset to a different collection.</summary>
    public void MoveToCollection(int collectionId)
    {
        CollectionId = collectionId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Check if this asset is owned by a specific user.</summary>
    public bool IsOwnedBy(string userId) => UserId == userId;

    /// <summary>Whether this is a system/shared asset (no owner).</summary>
    public bool IsSystemAsset => UserId == null;

    /// <summary>Mark this asset as soft-deleted.</summary>
    public void SoftDelete()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    // ── Clone support (internal — used by AssetFactory.Duplicate) ──

    /// <summary>
    /// Copy all shared base-class properties from a source asset.
    /// Subtype-specific properties are handled by overriding this method.
    /// </summary>
    internal virtual void InitializeClone(Asset source, string userId, string copySuffix, int? targetFolderId)
    {
        FileName = source.FileName + copySuffix;
        FilePath = source.FilePath;
#pragma warning disable CS0618
        Tags = source.Tags;
#pragma warning restore CS0618
        CollectionId = source.CollectionId;
        ContentType = source.ContentType;
        GroupId = source.GroupId;
        ParentFolderId = targetFolderId ?? source.ParentFolderId;
        SortOrder = source.SortOrder + 1;
        IsFolder = source.IsFolder;
        ThumbnailSm = source.ThumbnailSm;
        ThumbnailMd = source.ThumbnailMd;
        ThumbnailLg = source.ThumbnailLg;
        UserId = userId;
        CreatedAt = DateTime.UtcNow;
    }
}