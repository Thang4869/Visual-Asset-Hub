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
    private readonly AssetCleanupHelper _cleanup;
    private readonly ILogger<AssetService> _logger;
    private readonly IPermissionService _permissions;

    public AssetService(
        AppDbContext context,
        IStorageService storage,
        FileUploadConfig uploadConfig,
        IThumbnailService thumbnailService,
        INotificationService notifier,
        AssetCleanupHelper cleanup,
        ILogger<AssetService> logger,
        IPermissionService permissions)
    {
        _context = context;
        _storage = storage;
        _uploadConfig = uploadConfig;
        _thumbnailService = thumbnailService;
        _notifier = notifier;
        _cleanup = cleanup;
        _logger = logger;
        _permissions = permissions;
    }

    // ──── Private helpers ────

    /// <summary>
    /// Find an asset by ID, checking ownership first, then shared-collection permission.
    /// Throws <see cref="KeyNotFoundException"/> if not found or no access.
    /// </summary>
    private async Task<Asset> FindAssetWithAccessAsync(int id, string userId, string minimumRole, CancellationToken ct = default)
    {
        var asset = await _context.Assets.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new KeyNotFoundException("Asset not found.");

        // Owner always has full access
        if (asset.UserId == userId) return asset;

        // Check shared-collection permission
        if (await _permissions.HasPermissionAsync(asset.CollectionId, userId, minimumRole))
            return asset;

        throw new KeyNotFoundException("Asset not found.");
    }

    /// <summary>
    /// Resolve the owner userId for new assets in a collection.
    /// For shared collections, assets must belong to the collection owner.
    /// </summary>
    private async Task<string> ResolveAssetOwnerAsync(int collectionId, string actingUserId, CancellationToken ct = default)
    {
        var collection = await _context.Collections.FindAsync([collectionId], ct)
            ?? throw new KeyNotFoundException($"Collection {collectionId} not found.");

        // Owner or system collection
        if (collection.UserId == actingUserId || collection.UserId == null)
            return actingUserId;

        // Shared collection — need editor role
        if (await _permissions.HasPermissionAsync(collectionId, actingUserId, CollectionRoles.Editor))
            return collection.UserId; // asset belongs to collection owner

        throw new KeyNotFoundException($"Collection {collectionId} not found.");
    }

    // ──── IAssetService implementation ────

    public async Task<PagedResult<Asset>> GetAssetsAsync(PaginationParams pagination, string userId, CancellationToken ct = default)
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

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(ct);

        return new PagedResult<Asset>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    public async Task<Asset> GetByIdAsync(int id, string userId, CancellationToken ct = default)
    {
        // Consistent error pattern: throw KeyNotFoundException (→ 404 via middleware)
        return await FindAssetWithAccessAsync(id, userId, CollectionRoles.Viewer, ct);
    }

    public async Task<Asset> CreateAssetAsync(CreateAssetDto dto, string userId, CancellationToken ct = default)
    {
        var asset = AssetFactory.FromDto(dto, userId);
        _context.Assets.Add(asset);
        await _context.SaveChangesAsync(ct);
        await _notifier.NotifyAsync(userId, "AssetCreated", new { asset.Id, asset.FileName }, ct);
        return asset;
    }

    public async Task<List<Asset>> UploadFilesAsync(List<IFormFile> files, int collectionId, int? folderId, string userId, CancellationToken ct = default)
    {
        if (files == null || files.Count == 0)
            throw new ArgumentException("No files uploaded.");

        if (files.Count > _uploadConfig.MaxFilesPerRequest)
            throw new ArgumentException($"Maximum {_uploadConfig.MaxFilesPerRequest} files per request.");

        // Validate collection exists and user has access (own, system, or shared-editor)
        var collection = await _context.Collections.FindAsync([collectionId], ct);
        if (collection == null)
            throw new KeyNotFoundException($"Collection {collectionId} not found.");
        bool hasAccess = collection.UserId == userId || collection.UserId == null
            || await _permissions.HasPermissionAsync(collectionId, userId, CollectionRoles.Editor);
        if (!hasAccess)
            throw new KeyNotFoundException($"Collection {collectionId} not found.");

        // For shared collections, assets must be owned by the collection owner so they appear in the listing
        var assetOwner = collection.UserId ?? userId;

        var createdAssets = new List<Asset>();

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();

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
                ? (Asset)AssetFactory.CreateImage(file.FileName, filePath, collectionId, assetOwner, folderId)
                : AssetFactory.CreateFile(file.FileName, filePath, collectionId, assetOwner, folderId);

            _context.Assets.Add(asset);
            createdAssets.Add(asset);
        }

        await _context.SaveChangesAsync(ct);

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
            await _context.SaveChangesAsync(ct);

        await _notifier.NotifyAsync(userId, "AssetsUploaded", new { count = createdAssets.Count, collectionId }, ct);
        return createdAssets;
    }

    public async Task<Asset> UpdatePositionAsync(int id, double positionX, double positionY, string userId, CancellationToken ct = default)
    {
        var asset = await FindAssetWithAccessAsync(id, userId, CollectionRoles.Editor, ct);

        asset.UpdatePosition(positionX, positionY);
        await _context.SaveChangesAsync(ct);

        return asset;
    }

    public async Task<Asset> CreateFolderAsync(CreateFolderDto dto, string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.FolderName))
            throw new ArgumentException("Folder name is required.");

        var ownerId = await ResolveAssetOwnerAsync(dto.CollectionId, userId, ct);
        var folder = AssetFactory.CreateFolder(dto.FolderName.Trim(), dto.CollectionId, ownerId, dto.ParentFolderId);

        _context.Assets.Add(folder);
        await _context.SaveChangesAsync(ct);
        return folder;
    }

    public async Task<Asset> CreateColorAsync(CreateColorDto dto, string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.ColorCode))
            throw new ArgumentException("Color code is required.");

        // Normalize: auto-prepend # for hex color codes
        var code = dto.ColorCode.Trim();
        if (!code.StartsWith('#') && System.Text.RegularExpressions.Regex.IsMatch(code, @"^[0-9A-Fa-f]{3,8}$"))
            code = "#" + code;

        var ownerId = await ResolveAssetOwnerAsync(dto.CollectionId, userId, ct);
        var color = AssetFactory.CreateColor(
            code, dto.CollectionId, ownerId,
            dto.ColorName, dto.GroupId, dto.ParentFolderId, dto.SortOrder ?? 0);

        _context.Assets.Add(color);
        await _context.SaveChangesAsync(ct);
        return color;
    }

    public async Task<Asset> CreateColorGroupAsync(CreateColorGroupDto dto, string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.GroupName))
            throw new ArgumentException("Group name is required.");

        var ownerId = await ResolveAssetOwnerAsync(dto.CollectionId, userId, ct);
        var group = AssetFactory.CreateColorGroup(
            dto.GroupName.Trim(), dto.CollectionId, ownerId,
            dto.ParentFolderId, dto.SortOrder ?? 0);

        _context.Assets.Add(group);
        await _context.SaveChangesAsync(ct);
        return group;
    }

    public async Task<Asset> CreateLinkAsync(CreateLinkDto dto, string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Name is required.");
        if (string.IsNullOrWhiteSpace(dto.Url))
            throw new ArgumentException("URL is required.");

        // Basic URL validation
        if (!Uri.TryCreate(dto.Url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
            throw new ArgumentException("Invalid URL format. Must be http or https.");

        var ownerId = await ResolveAssetOwnerAsync(dto.CollectionId, userId, ct);
        var link = AssetFactory.CreateLink(
            dto.Name.Trim(), dto.Url.Trim(), dto.CollectionId, ownerId, dto.ParentFolderId);

        _context.Assets.Add(link);
        await _context.SaveChangesAsync(ct);
        return link;
    }

    public async Task<Asset> UpdateAssetAsync(int id, UpdateAssetDto dto, string userId, CancellationToken ct = default)
    {
        var asset = await FindAssetWithAccessAsync(id, userId, CollectionRoles.Editor, ct);

        asset.ApplyUpdate(dto);

        await _context.SaveChangesAsync(ct);
        return asset;
    }

    public async Task<bool> DeleteAssetAsync(int id, string userId, CancellationToken ct = default)
    {
        var asset = await FindAssetWithAccessAsync(id, userId, CollectionRoles.Editor, ct);

        // Clean up physical file and thumbnails via helper
        await _cleanup.CleanupFilesAsync(asset);

        // If deleting a folder, move children to parent folder (orphan prevention)
        if (asset.IsFolder)
        {
            var children = await _context.Assets
                .Where(a => a.ParentFolderId == id)
                .ToListAsync(ct);

            foreach (var child in children)
            {
                child.MoveToFolder(asset.ParentFolderId); // Move to grandparent
            }
        }

        _context.Assets.Remove(asset);
        await _context.SaveChangesAsync(ct);
        await _notifier.NotifyAsync(userId, "AssetDeleted", new { id }, ct);
        return true;
    }

    public async Task ReorderAssetsAsync(List<int> assetIds, string userId, CancellationToken ct = default)
    {
        if (assetIds == null || assetIds.Count == 0)
            throw new ArgumentException("Asset IDs are required.");

        // Batch fetch — user's own assets + shared-collection editor assets
        var allAssets = await _context.Assets
            .Where(a => assetIds.Contains(a.Id))
            .ToListAsync(ct);

        // Filter to assets the user can write (own or editor on collection)
        var assets = new List<Asset>();
        foreach (var a in allAssets)
        {
            if (a.UserId == userId || await _permissions.HasPermissionAsync(a.CollectionId, userId, CollectionRoles.Editor))
                assets.Add(a);
        }

        var assetMap = assets.ToDictionary(a => a.Id);

        for (int i = 0; i < assetIds.Count; i++)
        {
            if (assetMap.TryGetValue(assetIds[i], out var asset))
            {
                asset.SortOrder = i;
            }
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task<List<Asset>> GetAssetsByGroupAsync(int groupId, string userId, CancellationToken ct = default)
    {
        var candidates = await _context.Assets
            .Where(a => a.GroupId == groupId)
            .OrderBy(a => a.SortOrder)
            .ToListAsync(ct);

        // Return own assets, or assets the user can view via shared collection
        var result = new List<Asset>();
        foreach (var a in candidates)
        {
            if (a.UserId == userId || await _permissions.HasPermissionAsync(a.CollectionId, userId, CollectionRoles.Viewer))
                result.Add(a);
        }
        return result;
    }

    public async Task<Asset> DuplicateAssetAsync(int id, int? targetFolderId, string userId, CancellationToken ct = default)
    {
        var source = await FindAssetWithAccessAsync(id, userId, CollectionRoles.Editor, ct);

        var clone = AssetFactory.Duplicate(source, userId, targetFolderId);

        _context.Assets.Add(clone);
        await _context.SaveChangesAsync(ct);
        await _notifier.NotifyAsync(userId, "AssetCreated", new { clone.Id, clone.FileName }, ct);
        return clone;
    }
}
