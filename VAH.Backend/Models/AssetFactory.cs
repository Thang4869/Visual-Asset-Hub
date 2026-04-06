namespace VAH.Backend.Models;

/// <summary>
/// Factory for creating the correct Asset subtype.
/// Delegates construction to each subtype's internal constructor,
/// ensuring TPH discriminator consistency.
/// Accepts only primitive parameters — never DTOs.
/// </summary>
public static class AssetFactory
{
    // Backing instance used by the static facade so callers that still use
    // the static API get validation delegated through IAssetValidator.
    private static readonly IAssetFactory _impl = new StandardAssetFactory(new StandardAssetValidator());

    public static ImageAsset CreateImage(string fileName, string filePath, int collectionId, string userId, int? parentFolderId = null)
    {
        return _impl.CreateImage(fileName, filePath, collectionId, userId, parentFolderId);
    }

    public static FileAsset CreateFile(string fileName, string filePath, int collectionId, string userId, int? parentFolderId = null)
    {
        return _impl.CreateFile(fileName, filePath, collectionId, userId, parentFolderId);
    }

    public static FolderAsset CreateFolder(string name, int collectionId, string userId, int? parentFolderId = null)
    {
        return _impl.CreateFolder(name, collectionId, userId, parentFolderId);
    }

    public static ColorAsset CreateColor(string colorCode, int collectionId, string userId, string? colorName = null, int? groupId = null, int? parentFolderId = null, int sortOrder = 0)
    {
        return _impl.CreateColor(colorCode, collectionId, userId, colorName, groupId, parentFolderId, sortOrder);
    }

    public static ColorGroupAsset CreateColorGroup(string groupName, int collectionId, string userId, int? parentFolderId = null, int sortOrder = 0)
    {
        return _impl.CreateColorGroup(groupName, collectionId, userId, parentFolderId, sortOrder);
    }

    public static LinkAsset CreateLink(string name, string url, int collectionId, string userId, int? parentFolderId = null)
    {
        return _impl.CreateLink(name, url, collectionId, userId, parentFolderId);
    }

    /// <summary>
    /// Duplicate an existing asset, creating the correct TPH subtype.
    /// Copies all shared properties via <see cref="Asset.InitializeClone"/>;
    /// subtype-specific properties (Url, HexCode) are handled by overrides.
    /// </summary>
    /// <param name="copySuffix">Localized suffix appended to FileName (e.g. " (copy)", " (bản sao)").</param>
    public static Asset Duplicate(Asset source, string userId, string copySuffix, int? targetFolderId = null)
    {
        return _impl.Duplicate(source, userId, copySuffix, targetFolderId);
    }
}
