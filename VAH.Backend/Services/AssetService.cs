using Microsoft.EntityFrameworkCore;
using VAH.Backend.Data;
using VAH.Backend.Models;

namespace VAH.Backend.Services;

public class AssetService : IAssetService
{
    private readonly AppDbContext _context;
    private readonly IStorageService _storage;
    private readonly FileUploadConfig _uploadConfig;
    private readonly IThumbnailService _thumbnailService;
    private readonly INotificationService _notifier;
    private readonly ILogger<AssetService> _logger;

    public AssetService(
        AppDbContext context,
        IStorageService storage,
        FileUploadConfig uploadConfig,
        IThumbnailService thumbnailService,
        INotificationService notifier,
        ILogger<AssetService> logger)
    {
        _context = context;
        _storage = storage;
        _uploadConfig = uploadConfig;
        _thumbnailService = thumbnailService;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task<PagedResult<Asset>> GetAssetsAsync(PaginationParams pagination, string userId)
    {
        var query = _context.Assets
            .Where(a => a.UserId == userId)
            .AsQueryable();

        // Sorting
        query = pagination.SortBy?.ToLower() switch
        {
            "filename" => pagination.SortOrder == "desc"
                ? query.OrderByDescending(a => a.FileName)
                : query.OrderBy(a => a.FileName),
            "createdat" => pagination.SortOrder == "desc"
                ? query.OrderByDescending(a => a.CreatedAt)
                : query.OrderBy(a => a.CreatedAt),
            _ => query.OrderByDescending(a => a.CreatedAt) // default
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return new PagedResult<Asset>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    public async Task<Asset?> GetByIdAsync(int id, string userId)
    {
        return await _context.Assets
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
    }

    public async Task<Asset> CreateAssetAsync(Asset asset, string userId)
    {
        asset.CreatedAt = DateTime.UtcNow;
        asset.UserId = userId;
        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();
        await _notifier.NotifyAsync(userId, "AssetCreated", new { asset.Id, asset.FileName });
        return asset;
    }

    public async Task<List<Asset>> UploadFilesAsync(List<IFormFile> files, int collectionId, int? folderId, string userId)
    {
        if (files == null || files.Count == 0)
            throw new ArgumentException("No files uploaded.");

        if (files.Count > _uploadConfig.MaxFilesPerRequest)
            throw new ArgumentException($"Maximum {_uploadConfig.MaxFilesPerRequest} files per request.");

        // Validate collection exists and user has access (own or system)
        var collectionExists = await _context.Collections
            .AnyAsync(c => c.Id == collectionId && (c.UserId == userId || c.UserId == null));
        if (!collectionExists)
            throw new KeyNotFoundException($"Collection {collectionId} not found.");

        var createdAssets = new List<Asset>();

        foreach (var file in files)
        {
            // Validate file size
            if (file.Length > _uploadConfig.MaxFileSizeBytes)
                throw new ArgumentException(
                    $"File '{file.FileName}' exceeds maximum size of {_uploadConfig.MaxFileSizeBytes / (1024 * 1024)}MB.");

            if (file.Length == 0)
                continue;

            // Validate extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_uploadConfig.AllowedExtensions.Contains(extension))
                throw new ArgumentException(
                    $"File type '{extension}' is not allowed. Allowed: {string.Join(", ", _uploadConfig.AllowedExtensions)}");

            // Validate MIME type
            var isAllowedMime = _uploadConfig.AllowedMimeTypePrefixes
                .Any(prefix => file.ContentType?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true);
            if (!isAllowedMime && !string.IsNullOrEmpty(file.ContentType))
                _logger.LogWarning("Unexpected MIME type: {MimeType} for file {FileName}", file.ContentType, file.FileName);

            // Upload via storage service
            await using var stream = file.OpenReadStream();
            var filePath = await _storage.UploadAsync(stream, file.FileName, file.ContentType ?? "application/octet-stream");

            // Create correct subtype based on MIME type
            var asset = file.ContentType?.StartsWith("image") == true
                ? (Asset)AssetFactory.CreateImage(file.FileName, filePath, collectionId, userId, folderId)
                : AssetFactory.CreateFile(file.FileName, filePath, collectionId, userId, folderId);

            _context.Assets.Add(asset);
            createdAssets.Add(asset);
        }

        await _context.SaveChangesAsync();

        // Generate thumbnails for image assets (after SaveChanges so files are persisted)
        foreach (var asset in createdAssets.Where(a => a.CanHaveThumbnails))
        {
            try
            {
                var thumbs = await _thumbnailService.GenerateThumbnailsAsync(asset.FilePath);
                if (thumbs.Count > 0)
                {
                    asset.SetThumbnails(
                        thumbs.GetValueOrDefault("sm"),
                        thumbs.GetValueOrDefault("md"),
                        thumbs.GetValueOrDefault("lg"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Thumbnail generation failed for asset {AssetId}", asset.Id);
            }
        }

        // Save thumbnail paths
        if (createdAssets.Any(a => a.ThumbnailSm != null))
            await _context.SaveChangesAsync();

        await _notifier.NotifyAsync(userId, "AssetsUploaded", new { count = createdAssets.Count, collectionId });
        return createdAssets;
    }

    public async Task<Asset> UpdatePositionAsync(int id, double positionX, double positionY, string userId)
    {
        var asset = await _context.Assets
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId)
            ?? throw new KeyNotFoundException("Asset not found.");

        asset.UpdatePosition(positionX, positionY);
        await _context.SaveChangesAsync();

        return asset;
    }

    public async Task<Asset> CreateFolderAsync(CreateFolderDto dto, string userId)
    {
        if (string.IsNullOrWhiteSpace(dto.FolderName))
            throw new ArgumentException("Folder name is required.");

        var folder = AssetFactory.CreateFolder(dto.FolderName.Trim(), dto.CollectionId, userId, dto.ParentFolderId);

        _context.Assets.Add(folder);
        await _context.SaveChangesAsync();
        return folder;
    }

    public async Task<Asset> CreateColorAsync(CreateColorDto dto, string userId)
    {
        if (string.IsNullOrWhiteSpace(dto.ColorCode))
            throw new ArgumentException("Color code is required.");

        // Normalize: auto-prepend # for hex color codes
        var code = dto.ColorCode.Trim();
        if (!code.StartsWith('#') && System.Text.RegularExpressions.Regex.IsMatch(code, @"^[0-9A-Fa-f]{3,8}$"))
            code = "#" + code;

        var color = AssetFactory.CreateColor(
            code, dto.CollectionId, userId,
            dto.ColorName, dto.GroupId, dto.ParentFolderId, dto.SortOrder ?? 0);

        _context.Assets.Add(color);
        await _context.SaveChangesAsync();
        return color;
    }

    public async Task<Asset> CreateColorGroupAsync(CreateColorGroupDto dto, string userId)
    {
        if (string.IsNullOrWhiteSpace(dto.GroupName))
            throw new ArgumentException("Group name is required.");

        var group = AssetFactory.CreateColorGroup(
            dto.GroupName.Trim(), dto.CollectionId, userId,
            dto.ParentFolderId, dto.SortOrder ?? 0);

        _context.Assets.Add(group);
        await _context.SaveChangesAsync();
        return group;
    }

    public async Task<Asset> CreateLinkAsync(CreateLinkDto dto, string userId)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Name is required.");
        if (string.IsNullOrWhiteSpace(dto.Url))
            throw new ArgumentException("URL is required.");

        // Basic URL validation
        if (!Uri.TryCreate(dto.Url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
            throw new ArgumentException("Invalid URL format. Must be http or https.");

        var link = AssetFactory.CreateLink(
            dto.Name.Trim(), dto.Url.Trim(), dto.CollectionId, userId, dto.ParentFolderId);

        _context.Assets.Add(link);
        await _context.SaveChangesAsync();
        return link;
    }

    public async Task<Asset> UpdateAssetAsync(int id, UpdateAssetDto dto, string userId)
    {
        var asset = await _context.Assets
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId)
            ?? throw new KeyNotFoundException("Asset not found.");

        asset.ApplyUpdate(dto);

        await _context.SaveChangesAsync();
        return asset;
    }

    public async Task<bool> DeleteAssetAsync(int id, string userId)
    {
        var asset = await _context.Assets
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId)
            ?? throw new KeyNotFoundException("Asset not found.");

        // Clean up physical file and thumbnails if applicable
        if (asset.RequiresFileCleanup)
        {
            await _storage.DeleteAsync(asset.FilePath);

            if (!string.IsNullOrEmpty(asset.ThumbnailSm) && asset.ThumbnailSm != asset.FilePath)
                await _storage.DeleteAsync(asset.ThumbnailSm);
            if (!string.IsNullOrEmpty(asset.ThumbnailMd) && asset.ThumbnailMd != asset.FilePath)
                await _storage.DeleteAsync(asset.ThumbnailMd);
            if (!string.IsNullOrEmpty(asset.ThumbnailLg) && asset.ThumbnailLg != asset.FilePath)
                await _storage.DeleteAsync(asset.ThumbnailLg);
        }

        // If deleting a folder, move children to parent folder (orphan prevention)
        if (asset.IsFolder)
        {
            var children = await _context.Assets
                .Where(a => a.ParentFolderId == id)
                .ToListAsync();

            foreach (var child in children)
            {
                child.MoveToFolder(asset.ParentFolderId); // Move to grandparent
            }
        }

        _context.Assets.Remove(asset);
        await _context.SaveChangesAsync();
        await _notifier.NotifyAsync(userId, "AssetDeleted", new { id });
        return true;
    }

    public async Task ReorderAssetsAsync(List<int> assetIds, string userId)
    {
        if (assetIds == null || assetIds.Count == 0)
            throw new ArgumentException("Asset IDs are required.");

        // Batch fetch — only user's own assets
        var assets = await _context.Assets
            .Where(a => assetIds.Contains(a.Id) && a.UserId == userId)
            .ToListAsync();

        var assetMap = assets.ToDictionary(a => a.Id);

        for (int i = 0; i < assetIds.Count; i++)
        {
            if (assetMap.TryGetValue(assetIds[i], out var asset))
            {
                asset.SortOrder = i;
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<Asset>> GetAssetsByGroupAsync(int groupId, string userId)
    {
        return await _context.Assets
            .Where(a => a.GroupId == groupId && a.UserId == userId)
            .OrderBy(a => a.SortOrder)
            .ToListAsync();
    }

    // ──── Bulk Operations ────

    public async Task<int> BulkDeleteAsync(List<int> assetIds, string userId)
    {
        if (assetIds == null || assetIds.Count == 0)
            throw new ArgumentException("Asset IDs are required.");

        var assets = await _context.Assets
            .Where(a => assetIds.Contains(a.Id) && a.UserId == userId)
            .ToListAsync();

        foreach (var asset in assets)
        {
            // Clean up physical files and thumbnails if applicable
            if (asset.RequiresFileCleanup)
            {
                await _storage.DeleteAsync(asset.FilePath);
                if (!string.IsNullOrEmpty(asset.ThumbnailSm)) await _storage.DeleteAsync(asset.ThumbnailSm);
                if (!string.IsNullOrEmpty(asset.ThumbnailMd)) await _storage.DeleteAsync(asset.ThumbnailMd);
                if (!string.IsNullOrEmpty(asset.ThumbnailLg)) await _storage.DeleteAsync(asset.ThumbnailLg);
            }

            // Orphan prevention for folders
            if (asset.IsFolder)
            {
                var children = await _context.Assets
                    .Where(a => a.ParentFolderId == asset.Id)
                    .ToListAsync();
                foreach (var child in children)
                    child.MoveToFolder(asset.ParentFolderId);
            }
        }

        _context.Assets.RemoveRange(assets);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Bulk deleted {Count} assets for user {UserId}", assets.Count, userId);
        await _notifier.NotifyAsync(userId, "AssetsBulkDeleted", new { count = assets.Count, assetIds });
        return assets.Count;
    }

    public async Task<int> BulkMoveAsync(BulkMoveDto dto, string userId)
    {
        if (dto.AssetIds == null || dto.AssetIds.Count == 0)
            throw new ArgumentException("Asset IDs are required.");

        var assets = await _context.Assets
            .Where(a => dto.AssetIds.Contains(a.Id) && a.UserId == userId)
            .ToListAsync();

        // Validate target collection exists if specified
        if (dto.TargetCollectionId.HasValue)
        {
            var collExists = await _context.Collections
                .AnyAsync(c => c.Id == dto.TargetCollectionId.Value && (c.UserId == userId || c.UserId == null));
            if (!collExists)
                throw new KeyNotFoundException($"Target collection {dto.TargetCollectionId.Value} not found.");
        }

        foreach (var asset in assets)
        {
            if (dto.TargetCollectionId.HasValue)
                asset.MoveToCollection(dto.TargetCollectionId.Value);

            if (dto.ClearParentFolder == true)
                asset.MoveToFolder(null);
            else if (dto.TargetFolderId.HasValue)
                asset.MoveToFolder(dto.TargetFolderId.Value);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Bulk moved {Count} assets for user {UserId}", assets.Count, userId);
        await _notifier.NotifyAsync(userId, "AssetsBulkMoved", new { count = assets.Count });
        return assets.Count;
    }

    public async Task<int> BulkTagAsync(BulkTagDto dto, string userId)
    {
        if (dto.AssetIds == null || dto.AssetIds.Count == 0)
            throw new ArgumentException("Asset IDs are required.");
        if (dto.TagIds == null || dto.TagIds.Count == 0)
            throw new ArgumentException("Tag IDs are required.");

        // Verify all assets belong to user
        var assets = await _context.Assets
            .Where(a => dto.AssetIds.Contains(a.Id) && a.UserId == userId)
            .ToListAsync();

        // Verify tags belong to user
        var validTagIds = await _context.Tags
            .Where(t => dto.TagIds.Contains(t.Id) && t.UserId == userId)
            .Select(t => t.Id)
            .ToListAsync();

        int count = 0;

        foreach (var asset in assets)
        {
            foreach (var tagId in validTagIds)
            {
                if (dto.Remove)
                {
                    var junction = await _context.AssetTags
                        .FirstOrDefaultAsync(at => at.AssetId == asset.Id && at.TagId == tagId);
                    if (junction != null)
                    {
                        _context.AssetTags.Remove(junction);
                        count++;
                    }
                }
                else
                {
                    var exists = await _context.AssetTags
                        .AnyAsync(at => at.AssetId == asset.Id && at.TagId == tagId);
                    if (!exists)
                    {
                        _context.AssetTags.Add(new AssetTag { AssetId = asset.Id, TagId = tagId });
                        count++;
                    }
                }
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Bulk tag operation ({Action}) on {Count} asset-tag pairs for user {UserId}",
            dto.Remove ? "remove" : "add", count, userId);
        return count;
    }
}
