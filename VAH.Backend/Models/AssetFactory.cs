namespace VAH.Backend.Models;

/// <summary>
/// Factory for creating the correct Asset subtype.
/// Delegates construction to each subtype's internal constructor,
/// ensuring TPH discriminator consistency.
/// Accepts only primitive parameters — never DTOs.
/// </summary>
public static class AssetFactory
{
    public static ImageAsset CreateImage(string fileName, string filePath, int collectionId, string userId, int? parentFolderId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return new(fileName, filePath, collectionId, userId, parentFolderId);
    }

    public static FileAsset CreateFile(string fileName, string filePath, int collectionId, string userId, int? parentFolderId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return new(fileName, filePath, collectionId, userId, parentFolderId);
    }

    public static FolderAsset CreateFolder(string name, int collectionId, string userId, int? parentFolderId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return new(name, collectionId, userId, parentFolderId);
    }

    public static ColorAsset CreateColor(string colorCode, int collectionId, string userId, string? colorName = null, int? groupId = null, int? parentFolderId = null, int sortOrder = 0)
    {
        var normalizedCode = AssetValidator.NormalizeHexColor(colorCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return new(normalizedCode, collectionId, userId, colorName, groupId, parentFolderId, sortOrder);
    }

    public static ColorGroupAsset CreateColorGroup(string groupName, int collectionId, string userId, int? parentFolderId = null, int sortOrder = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return new(groupName, collectionId, userId, parentFolderId, sortOrder);
    }

    public static LinkAsset CreateLink(string name, string url, int collectionId, string userId, int? parentFolderId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var validatedUrl = AssetValidator.ValidateUrl(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return new(name, validatedUrl, collectionId, userId, parentFolderId);
    }

    /// <summary>
    /// Duplicate an existing asset, creating the correct TPH subtype.
    /// Copies all shared properties via <see cref="Asset.InitializeClone"/>;
    /// subtype-specific properties (Url, HexCode) are handled by overrides.
    /// </summary>
    /// <param name="copySuffix">Localized suffix appended to FileName (e.g. " (copy)", " (bản sao)").</param>
    public static Asset Duplicate(Asset source, string userId, string copySuffix, int? targetFolderId = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        // Create correct TPH subtype (parameterless internal constructors)
        Asset clone = source.ContentType switch
        {
            AssetContentType.Image => new ImageAsset(),
            AssetContentType.Link => new LinkAsset(),
            AssetContentType.Color => new ColorAsset(),
            AssetContentType.ColorGroup => new ColorGroupAsset(),
            AssetContentType.Folder => new FolderAsset(),
            _ => new FileAsset(),
        };

        // Virtual dispatch ensures subtype-specific props are copied
        clone.InitializeClone(source, userId, copySuffix, targetFolderId);

        return clone;
    }
}
