namespace VAH.Backend.Models;

/// <summary>
/// Factory for creating the correct Asset subtype.
/// Ensures each content type is instantiated as its proper TPH class.
/// </summary>
public static class AssetFactory
{
    public static ImageAsset CreateImage(string fileName, string filePath, int collectionId, string userId, int? parentFolderId = null) => new()
    {
        FileName = fileName,
        FilePath = filePath,
        Tags = string.Empty,
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
        CollectionId = collectionId,
        ParentFolderId = parentFolderId,
        SortOrder = 0,
        CreatedAt = DateTime.UtcNow,
        UserId = userId,
    };
}
