using System;

namespace VAH.Backend.Models;

/// <summary>
/// Instance implementation of <see cref="IAssetFactory"/> that delegates
/// validation to an injected <see cref="IAssetValidator"/>. This allows
/// runtime policy changes and makes the factory testable.
/// </summary>
public class StandardAssetFactory : IAssetFactory
{
    private readonly IAssetValidator _validator;

    public StandardAssetFactory(IAssetValidator validator)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public ImageAsset CreateImage(string fileName, string filePath, int collectionId, string userId, int? parentFolderId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return new(fileName, filePath, collectionId, userId, parentFolderId);
    }

    public FileAsset CreateFile(string fileName, string filePath, int collectionId, string userId, int? parentFolderId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return new(fileName, filePath, collectionId, userId, parentFolderId);
    }

    public FolderAsset CreateFolder(string name, int collectionId, string userId, int? parentFolderId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return new(name, collectionId, userId, parentFolderId);
    }

    public ColorAsset CreateColor(string colorCode, int collectionId, string userId, string? colorName = null, int? groupId = null, int? parentFolderId = null, int sortOrder = 0)
    {
        var normalizedCode = _validator.NormalizeHexColor(colorCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return new(normalizedCode, collectionId, userId, colorName, groupId, parentFolderId, sortOrder);
    }

    public ColorGroupAsset CreateColorGroup(string groupName, int collectionId, string userId, int? parentFolderId = null, int sortOrder = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return new(groupName, collectionId, userId, parentFolderId, sortOrder);
    }

    public LinkAsset CreateLink(string name, string url, int collectionId, string userId, int? parentFolderId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var validatedUrl = _validator.ValidateUrl(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return new(name, validatedUrl, collectionId, userId, parentFolderId);
    }

    public Asset Duplicate(Asset source, string userId, string copySuffix, int? targetFolderId = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        Asset clone = source.ContentType switch
        {
            AssetContentType.Image => new ImageAsset(),
            AssetContentType.Link => new LinkAsset(),
            AssetContentType.Color => new ColorAsset(),
            AssetContentType.ColorGroup => new ColorGroupAsset(),
            AssetContentType.Folder => new FolderAsset(),
            _ => new FileAsset(),
        };

        clone.InitializeClone(source, userId, copySuffix, targetFolderId);

        return clone;
    }
}
