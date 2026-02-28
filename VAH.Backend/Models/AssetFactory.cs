namespace VAH.Backend.Models;

/// <summary>
/// Factory for creating the correct Asset subtype.
/// Ensures each content type is instantiated as its proper TPH class.
/// ContentType is set explicitly so the in-memory object matches the DB discriminator.
/// </summary>
public static class AssetFactory
{
    public static ImageAsset CreateImage(string fileName, string filePath, int collectionId, string userId, int? parentFolderId = null) => new()
    {
        FileName = fileName,
        FilePath = filePath,
        Tags = string.Empty,
        ContentType = AssetContentType.Image,
        CollectionId = collectionId,
        ParentFolderId = parentFolderId,
        CreatedAt = DateTime.UtcNow,
        UserId = userId,
    };

    public static Asset CreateFile(string fileName, string filePath, int collectionId, string userId, int? parentFolderId = null) => new()
    {
        FileName = fileName,
        FilePath = filePath,
        Tags = string.Empty,
        ContentType = AssetContentType.File,
        CollectionId = collectionId,
        ParentFolderId = parentFolderId,
        CreatedAt = DateTime.UtcNow,
        UserId = userId,
    };

    public static FolderAsset CreateFolder(string name, int collectionId, string userId, int? parentFolderId = null) => new()
    {
        FileName = name,
        FilePath = string.Empty,
        Tags = string.Empty,
        ContentType = AssetContentType.Folder,
        IsFolder = true,
        CollectionId = collectionId,
        ParentFolderId = parentFolderId,
        SortOrder = 0,
        CreatedAt = DateTime.UtcNow,
        UserId = userId,
    };

    public static ColorAsset CreateColor(string colorCode, int collectionId, string userId, string? colorName = null, int? groupId = null, int? parentFolderId = null, int sortOrder = 0) => new()
    {
        FileName = colorCode,
        FilePath = colorCode,
        Tags = colorName ?? colorCode,
        ContentType = AssetContentType.Color,
        CollectionId = collectionId,
        GroupId = groupId,
        ParentFolderId = parentFolderId,
        SortOrder = sortOrder,
        CreatedAt = DateTime.UtcNow,
        UserId = userId,
    };

    public static ColorGroupAsset CreateColorGroup(string groupName, int collectionId, string userId, int? parentFolderId = null, int sortOrder = 0) => new()
    {
        FileName = groupName,
        FilePath = string.Empty,
        Tags = string.Empty,
        ContentType = AssetContentType.ColorGroup,
        CollectionId = collectionId,
        ParentFolderId = parentFolderId,
        SortOrder = sortOrder,
        CreatedAt = DateTime.UtcNow,
        UserId = userId,
    };

    public static LinkAsset CreateLink(string name, string url, int collectionId, string userId, int? parentFolderId = null) => new()
    {
        FileName = name,
        FilePath = url,
        Tags = string.Empty,
        ContentType = AssetContentType.Link,
        CollectionId = collectionId,
        ParentFolderId = parentFolderId,
        SortOrder = 0,
        CreatedAt = DateTime.UtcNow,
        UserId = userId,
    };

    /// <summary>
    /// Duplicate an existing asset, creating the correct TPH subtype.
    /// Copies all shared properties; caller can override specific fields afterward.
    /// </summary>
    public static Asset Duplicate(Asset source, string userId, int? targetFolderId = null)
    {
        // Create correct TPH subtype so EF Core sets the discriminator properly
        Asset clone = source.ContentType switch
        {
            AssetContentType.Image => new ImageAsset(),
            AssetContentType.Link => new LinkAsset(),
            AssetContentType.Color => new ColorAsset(),
            AssetContentType.ColorGroup => new ColorGroupAsset(),
            AssetContentType.Folder => new FolderAsset(),
            _ => new Asset(),
        };

        clone.FileName = source.FileName + " (bản sao)";
        clone.FilePath = source.FilePath;
        clone.Tags = source.Tags;
        clone.CreatedAt = DateTime.UtcNow;
        clone.CollectionId = source.CollectionId;
        clone.ContentType = source.ContentType;
        clone.GroupId = source.GroupId;
        clone.ParentFolderId = targetFolderId ?? source.ParentFolderId;
        clone.SortOrder = source.SortOrder + 1;
        clone.IsFolder = source.IsFolder;
        clone.UserId = userId;
        clone.ThumbnailSm = source.ThumbnailSm;
        clone.ThumbnailMd = source.ThumbnailMd;
        clone.ThumbnailLg = source.ThumbnailLg;

        return clone;
    }

    /// <summary>
    /// Create an Asset from a CreateAssetDto (generic file).
    /// </summary>
    public static Asset FromDto(CreateAssetDto dto, string userId) => new()
    {
        FileName = dto.FileName.Trim(),
        FilePath = dto.FilePath.Trim(),
        Tags = string.Empty,
        ContentType = AssetContentType.File,
        CollectionId = dto.CollectionId,
        ParentFolderId = dto.ParentFolderId,
        CreatedAt = DateTime.UtcNow,
        UserId = userId,
    };
}
