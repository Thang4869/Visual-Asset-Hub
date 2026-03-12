using System.ComponentModel.DataAnnotations;

namespace VAH.Backend.Models;

// ──────────────────────────────────────────────────────────────
//  TPH (Table-Per-Hierarchy) subclasses of Asset.
//  EF Core materializes the correct subtype based on the
//  ContentType discriminator column.
//  All leaf types are sealed — design intent + JIT devirtualization.
//  Each type has: internal() for EF Core / Duplicate,
//  internal(...) for AssetFactory construction.
// ──────────────────────────────────────────────────────────────

/// <summary>Generic file asset — any uploaded file that isn't a recognized image.</summary>
public sealed class FileAsset : Asset
{
    internal FileAsset() { }

    internal FileAsset(string fileName, string filePath, int collectionId, string userId, int? parentFolderId = null)
        : base(fileName, filePath, AssetContentType.File, collectionId, userId, parentFolderId) { }

    public override bool HasPhysicalFile => true;
}

/// <summary>Image asset — uploaded image file with thumbnail support.</summary>
public sealed class ImageAsset : Asset
{
    internal ImageAsset() { }

    internal ImageAsset(string fileName, string filePath, int collectionId, string userId, int? parentFolderId = null)
        : base(fileName, filePath, AssetContentType.Image, collectionId, userId, parentFolderId) { }

    public override bool HasPhysicalFile => true;
    public override bool CanHaveThumbnails => true;
}

/// <summary>Link/bookmark asset — stores a URL reference.</summary>
public sealed class LinkAsset : Asset
{
    internal LinkAsset() { }

    internal LinkAsset(string name, string url, int collectionId, string userId, int? parentFolderId = null)
        : base(name, url, AssetContentType.Link, collectionId, userId, parentFolderId)
    {
        Url = url;
    }

    /// <summary>The bookmarked URL. Stored separately from FilePath for semantic clarity.</summary>
    [MaxLength(2048)]
    public string? Url { get; private set; }

    public override bool HasPhysicalFile => false;

    /// <summary>Validate that the URL is a well-formed absolute http(s) URI.</summary>
    public bool IsValidUrl() =>
        !string.IsNullOrWhiteSpace(Url)
        && Uri.TryCreate(Url, UriKind.Absolute, out var uri)
        && (uri.Scheme == "http" || uri.Scheme == "https");

    internal override void InitializeClone(Asset source, string userId, string copySuffix, int? targetFolderId)
    {
        base.InitializeClone(source, userId, copySuffix, targetFolderId);
        Url = source is LinkAsset link ? link.Url : source.FilePath;
    }
}

/// <summary>Color swatch asset — stores a hex color code.</summary>
public sealed class ColorAsset : Asset
{
    internal ColorAsset() { }

    internal ColorAsset(string colorCode, int collectionId, string userId,
        string? colorName = null, int? groupId = null, int? parentFolderId = null, int sortOrder = 0)
        : base(colorCode, colorCode, AssetContentType.Color, collectionId, userId, parentFolderId, groupId, sortOrder)
    {
        HexCode = colorCode;
#pragma warning disable CS0618
        Tags = colorName ?? colorCode;
#pragma warning restore CS0618
    }

    /// <summary>The hex color code (e.g. #FF5733). Stored separately for semantic clarity.</summary>
    [MaxLength(50)]
    public string? HexCode { get; private set; }

    public override bool HasPhysicalFile => false;

    internal override void InitializeClone(Asset source, string userId, string copySuffix, int? targetFolderId)
    {
        base.InitializeClone(source, userId, copySuffix, targetFolderId);
        HexCode = source is ColorAsset color ? color.HexCode : source.FilePath;
    }
}

/// <summary>Color group — organizes color swatches together.</summary>
public sealed class ColorGroupAsset : Asset
{
    internal ColorGroupAsset() { }

    internal ColorGroupAsset(string groupName, int collectionId, string userId,
        int? parentFolderId = null, int sortOrder = 0)
        : base(groupName, string.Empty, AssetContentType.ColorGroup, collectionId, userId, parentFolderId, sortOrder: sortOrder) { }

    public override bool HasPhysicalFile => false;
}

/// <summary>Folder — organizes assets hierarchically within a collection.</summary>
public sealed class FolderAsset : Asset
{
    internal FolderAsset() { }

    internal FolderAsset(string name, int collectionId, string userId, int? parentFolderId = null)
        : base(name, string.Empty, AssetContentType.Folder, collectionId, userId, parentFolderId, isFolder: true) { }

    public override bool HasPhysicalFile => false;
}
