namespace VAH.Backend.Models;

public interface IAssetFactory
{
    ImageAsset CreateImage(string fileName, string filePath, int collectionId, string userId, int? parentFolderId = null);
    FileAsset CreateFile(string fileName, string filePath, int collectionId, string userId, int? parentFolderId = null);
    FolderAsset CreateFolder(string name, int collectionId, string userId, int? parentFolderId = null);
    ColorAsset CreateColor(string colorCode, int collectionId, string userId, string? colorName = null, int? groupId = null, int? parentFolderId = null, int sortOrder = 0);
    ColorGroupAsset CreateColorGroup(string groupName, int collectionId, string userId, int? parentFolderId = null, int sortOrder = 0);
    LinkAsset CreateLink(string name, string url, int collectionId, string userId, int? parentFolderId = null);
    Asset Duplicate(Asset source, string userId, string copySuffix, int? targetFolderId = null);
}
