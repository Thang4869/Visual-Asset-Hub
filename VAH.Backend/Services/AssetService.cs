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
    private readonly ILogger<AssetService> _logger;

    public AssetService(
        AppDbContext context,
        IStorageService storage,
        FileUploadConfig uploadConfig,
        IThumbnailService thumbnailService,
        ILogger<AssetService> logger)
    {
        _context = context;
        _storage = storage;
        _uploadConfig = uploadConfig;
        _thumbnailService = thumbnailService;
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

            // Determine content type
            var contentType = file.ContentType?.StartsWith("image") == true ? "image" : "file";

            var asset = new Asset
            {
                FileName = file.FileName,
                FilePath = filePath,
                Tags = string.Empty,
                CreatedAt = DateTime.UtcNow,
                CollectionId = collectionId,
                ContentType = contentType,
                ParentFolderId = folderId,
                UserId = userId
            };

            _context.Assets.Add(asset);
            createdAssets.Add(asset);
        }

        await _context.SaveChangesAsync();

        // Generate thumbnails for image assets (after SaveChanges so files are persisted)
        foreach (var asset in createdAssets.Where(a => a.ContentType == "image"))
        {
            try
            {
                var thumbs = await _thumbnailService.GenerateThumbnailsAsync(asset.FilePath);
                if (thumbs.Count > 0)
                {
                    asset.ThumbnailSm = thumbs.GetValueOrDefault("sm");
                    asset.ThumbnailMd = thumbs.GetValueOrDefault("md");
                    asset.ThumbnailLg = thumbs.GetValueOrDefault("lg");
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

        return createdAssets;
    }

    public async Task<Asset> UpdatePositionAsync(int id, double positionX, double positionY, string userId)
    {
        var asset = await _context.Assets
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId)
            ?? throw new KeyNotFoundException("Asset not found.");

        asset.PositionX = positionX;
        asset.PositionY = positionY;
        await _context.SaveChangesAsync();

        return asset;
    }

    public async Task<Asset> CreateFolderAsync(CreateFolderDto dto, string userId)
    {
        if (string.IsNullOrWhiteSpace(dto.FolderName))
            throw new ArgumentException("Folder name is required.");

        var folder = new Asset
        {
            FileName = dto.FolderName.Trim(),
            FilePath = string.Empty,
            Tags = string.Empty,
            ContentType = "folder",
            IsFolder = true,
            CollectionId = dto.CollectionId,
            ParentFolderId = dto.ParentFolderId,
            SortOrder = 0,
            CreatedAt = DateTime.UtcNow,
            UserId = userId
        };

        _context.Assets.Add(folder);
        await _context.SaveChangesAsync();
        return folder;
    }

    public async Task<Asset> CreateColorAsync(CreateColorDto dto, string userId)
    {
        if (string.IsNullOrWhiteSpace(dto.ColorCode))
            throw new ArgumentException("Color code is required.");

        var color = new Asset
        {
            FileName = dto.ColorCode.Trim(),
            FilePath = dto.ColorCode.Trim(),
            Tags = dto.ColorName ?? dto.ColorCode.Trim(),
            ContentType = "color",
            CollectionId = dto.CollectionId,
            GroupId = dto.GroupId,
            ParentFolderId = dto.ParentFolderId,
            SortOrder = dto.SortOrder ?? 0,
            CreatedAt = DateTime.UtcNow,
            UserId = userId
        };

        _context.Assets.Add(color);
        await _context.SaveChangesAsync();
        return color;
    }

    public async Task<Asset> CreateColorGroupAsync(CreateColorGroupDto dto, string userId)
    {
        if (string.IsNullOrWhiteSpace(dto.GroupName))
            throw new ArgumentException("Group name is required.");

        var group = new Asset
        {
            FileName = dto.GroupName.Trim(),
            FilePath = string.Empty,
            Tags = string.Empty,
            ContentType = "color-group",
            IsFolder = false,
            CollectionId = dto.CollectionId,
            ParentFolderId = dto.ParentFolderId,
            SortOrder = dto.SortOrder ?? 0,
            CreatedAt = DateTime.UtcNow,
            UserId = userId
        };

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

        var link = new Asset
        {
            FileName = dto.Name.Trim(),
            FilePath = dto.Url.Trim(),
            Tags = string.Empty,
            ContentType = "link",
            CollectionId = dto.CollectionId,
            ParentFolderId = dto.ParentFolderId,
            SortOrder = 0,
            CreatedAt = DateTime.UtcNow,
            UserId = userId
        };

        _context.Assets.Add(link);
        await _context.SaveChangesAsync();
        return link;
    }

    public async Task<Asset> UpdateAssetAsync(int id, UpdateAssetDto dto, string userId)
    {
        var asset = await _context.Assets
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId)
            ?? throw new KeyNotFoundException("Asset not found.");

        if (!string.IsNullOrEmpty(dto.FileName))
            asset.FileName = dto.FileName.Trim();
        if (dto.SortOrder.HasValue)
            asset.SortOrder = dto.SortOrder.Value;
        if (dto.GroupId.HasValue)
            asset.GroupId = dto.GroupId.Value;
        if (dto.ParentFolderId.HasValue)
            asset.ParentFolderId = dto.ParentFolderId.Value;
        if (dto.ClearParentFolder == true)
            asset.ParentFolderId = null;

        await _context.SaveChangesAsync();
        return asset;
    }

    public async Task<bool> DeleteAssetAsync(int id, string userId)
    {
        var asset = await _context.Assets
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId)
            ?? throw new KeyNotFoundException("Asset not found.");

        // Clean up physical file if it's an uploaded file (not a folder, link, or color)
        if (!asset.IsFolder &&
            asset.ContentType != "link" &&
            asset.ContentType != "color" &&
            asset.ContentType != "color-group" &&
            !string.IsNullOrEmpty(asset.FilePath) &&
            asset.FilePath.StartsWith("/uploads/"))
        {
            await _storage.DeleteAsync(asset.FilePath);

            // Also clean up thumbnails
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
                child.ParentFolderId = asset.ParentFolderId; // Move to grandparent
            }
        }

        _context.Assets.Remove(asset);
        await _context.SaveChangesAsync();
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
}
