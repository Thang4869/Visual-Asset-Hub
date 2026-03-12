namespace VAH.Backend.Services;

/// <summary>
/// Encapsulates asset file and thumbnail cleanup logic.
/// Eliminates duplication between DeleteAssetAsync and BulkDeleteAsync.
/// OOP: Single Responsibility — cleanup is a separate concern from CRUD orchestration.
/// File cleanup eligibility is determined here (service layer), not in the domain model.
/// </summary>
public class AssetCleanupHelper
{
    private readonly IStorageService _storage;
    private readonly ILogger<AssetCleanupHelper> _logger;

    public AssetCleanupHelper(IStorageService storage, ILogger<AssetCleanupHelper> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    /// <summary>
    /// Whether a physical file cleanup is needed for this asset.
    /// Determined by storage service (supports local, S3, Azure, etc.).
    /// </summary>
    public bool RequiresFileCleanup(Models.Asset asset) =>
        asset.HasPhysicalFile
        && !string.IsNullOrEmpty(asset.FilePath)
        && _storage.Exists(asset.FilePath);

    /// <summary>
    /// Delete the physical file and any generated thumbnails for an asset.
    /// </summary>
    public async Task CleanupFilesAsync(Models.Asset asset)
    {
        if (!RequiresFileCleanup(asset)) return;

        try
        {
            await _storage.DeleteAsync(asset.FilePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete file {FilePath} for asset {AssetId}", asset.FilePath, asset.Id);
        }

        await CleanupThumbnailsAsync(asset);
    }

    /// <summary>
    /// Delete generated thumbnails (sm, md, lg) for an asset.
    /// </summary>
    public async Task CleanupThumbnailsAsync(Models.Asset asset)
    {
        await TryDeleteAsync(asset.ThumbnailSm, asset.FilePath, asset.Id);
        await TryDeleteAsync(asset.ThumbnailMd, asset.FilePath, asset.Id);
        await TryDeleteAsync(asset.ThumbnailLg, asset.FilePath, asset.Id);
    }

    private async Task TryDeleteAsync(string? thumbnailPath, string? originalPath, int assetId)
    {
        if (string.IsNullOrEmpty(thumbnailPath) || thumbnailPath == originalPath)
            return;

        try
        {
            await _storage.DeleteAsync(thumbnailPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete thumbnail {Path} for asset {AssetId}", thumbnailPath, assetId);
        }
    }
}
