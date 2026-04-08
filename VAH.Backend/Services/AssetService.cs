#pragma warning disable CS8602
#pragma warning disable CS8602
using System.Collections.Generic;
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
    private readonly IAssetValidator _assetValidator;
    private readonly IAssetFactory _assetFactory;
    private readonly IAssetMapper _assetMapper;

    public AssetService(
        AppDbContext context,
        IStorageService storage,
        FileUploadConfig uploadConfig,
        IThumbnailService thumbnailService,
        INotificationService notifier,
        AssetCleanupHelper cleanup,
        ILogger<AssetService> logger,
        IPermissionService permissions,
        IAssetValidator assetValidator,
        IAssetFactory assetFactory,
        IAssetMapper assetMapper)
    {
        _context = context;
        _storage = storage;
        _uploadConfig = uploadConfig;
        _thumbnailService = thumbnailService;
        _notifier = notifier;
        _cleanup = cleanup;
        _logger = logger;
        _permissions = permissions;
        _assetValidator = assetValidator;
        _assetFactory = assetFactory;
        _assetMapper = assetMapper;
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

    public async Task<PagedResult<AssetResponseDto>> GetAssetsAsync(PaginationParams pagination, string userId, CancellationToken ct = default)
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

        return new PagedResult<AssetResponseDto>
        {
            Items = _assetMapper.ToDtoList(items),
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    public async Task<AssetResponseDto> GetByIdAsync(int id, string userId, CancellationToken ct = default)
    {
        var asset = await FindAssetWithAccessAsync(id, userId, CollectionRoles.Viewer, ct);
        return _assetMapper.ToDto(asset);
    }

    public async Task<AssetResponseDto> CreateAssetAsync(CreateAssetDto dto, string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.FileName))
            throw new ArgumentException("File name is required.");

        var validatedFileName = _assetValidator.ValidateFileName(dto.FileName.Trim());
        var filePath = dto.FilePath?.Trim() ?? throw new ArgumentException("File path is required.");
        var asset = _assetFactory.CreateFile(validatedFileName, filePath, dto.CollectionId, userId, dto.ParentFolderId);

        _context.Assets.Add(asset);
        await _context.SaveChangesAsync(ct);
        await _notifier.NotifyAsync(userId, "AssetCreated", new { asset.Id, asset.FileName }, ct);
        return _assetMapper.ToDto(asset);
    }

    public async Task<IReadOnlyList<AssetResponseDto>> UploadFilesAsync(IReadOnlyCollection<UploadedFileDto> files, int collectionId, int? folderId, string userId, CancellationToken ct = default)
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

            // Validate file name and extension
            var validatedFileName = _assetValidator.ValidateFileName(file.FileName);
            var extension = Path.GetExtension(validatedFileName).ToLowerInvariant();
            if (!_uploadConfig.AllowedExtensions.Contains(extension))
                throw new ArgumentException(
                    $"File type '{extension}' is not allowed. Allowed: {string.Join(", ", _uploadConfig.AllowedExtensions)}");

            // Validate MIME type
            var contentType = file.ContentType;
            var isAllowedMime = _uploadConfig.AllowedMimeTypePrefixes
                .Any(prefix => contentType?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true);
            if (!isAllowedMime && !string.IsNullOrEmpty(contentType))
                _logger.LogWarning("Unexpected MIME type: {MimeType} for file {FileName}", file.ContentType, file.FileName);

            // Upload via storage service
            await using var stream = file.OpenStream();
            var filePath = await _storage.UploadAsync(stream, validatedFileName, contentType ?? "application/octet-stream", ct);

            // Create correct subtype based on MIME type
            var asset = contentType?.StartsWith("image") == true
                ? (Asset)_assetFactory.CreateImage(validatedFileName, filePath, collectionId, assetOwner, folderId)
                : _assetFactory.CreateFile(validatedFileName, filePath, collectionId, assetOwner, folderId);

            _context.Assets.Add(asset);
            createdAssets.Add(asset);
        }

        await _context.SaveChangesAsync(ct);

        // Generate thumbnails for image assets (after SaveChanges so files are persisted)
        foreach (var asset in createdAssets.Where(a => a.CanHaveThumbnails))
        {
            try
            {
                var thumbs = await _thumbnailService.GenerateThumbnailsAsync(asset.FilePath, ct);
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
        return _assetMapper.ToDtoList(createdAssets);
    }

    public async Task<AssetResponseDto> UpdatePositionAsync(int id, double positionX, double positionY, string userId, CancellationToken ct = default)
    {
        var asset = await FindAssetWithAccessAsync(id, userId, CollectionRoles.Editor, ct);

        asset.UpdatePosition(positionX, positionY);
        await _context.SaveChangesAsync(ct);

        return _assetMapper.ToDto(asset);
    }

    public async Task<AssetResponseDto> CreateFolderAsync(CreateFolderDto dto, string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.FolderName))
            throw new ArgumentException("Folder name is required.");

        var validatedName = _assetValidator.ValidateFileName(dto.FolderName.Trim());
        var ownerId = await ResolveAssetOwnerAsync(dto.CollectionId, userId, ct);
        var folder = _assetFactory.CreateFolder(validatedName, dto.CollectionId, ownerId, dto.ParentFolderId);

        _context.Assets.Add(folder);
        await _context.SaveChangesAsync(ct);
        return _assetMapper.ToDto(folder);
    }

    public async Task<AssetResponseDto> CreateColorAsync(CreateColorDto dto, string userId, CancellationToken ct = default)
    {
        var ownerId = await ResolveAssetOwnerAsync(dto.CollectionId, userId, ct);
        var normalized = _assetValidator.NormalizeHexColor(dto.ColorCode);
        var color = _assetFactory.CreateColor(
            normalized, dto.CollectionId, ownerId,
            dto.ColorName, dto.GroupId, dto.ParentFolderId, dto.SortOrder ?? 0);

        _context.Assets.Add(color);
        await _context.SaveChangesAsync(ct);
        return _assetMapper.ToDto(color);
    }

    public async Task<AssetResponseDto> CreateColorGroupAsync(CreateColorGroupDto dto, string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.GroupName))
            throw new ArgumentException("Group name is required.");

        var validatedGroupName = _assetValidator.ValidateFileName(dto.GroupName.Trim());
        var ownerId = await ResolveAssetOwnerAsync(dto.CollectionId, userId, ct);
        var group = _assetFactory.CreateColorGroup(
            validatedGroupName, dto.CollectionId, ownerId,
            dto.ParentFolderId, dto.SortOrder ?? 0);

        _context.Assets.Add(group);
        await _context.SaveChangesAsync(ct);
        return _assetMapper.ToDto(group);
    }

    public async Task<AssetResponseDto> CreateLinkAsync(CreateLinkDto dto, string userId, CancellationToken ct = default)
    {
        var ownerId = await ResolveAssetOwnerAsync(dto.CollectionId, userId, ct);
        var validatedName = _assetValidator.ValidateFileName(dto.Name ?? string.Empty);
        var validatedUrl = _assetValidator.ValidateUrl(dto.Url);
        var link = _assetFactory.CreateLink(
            validatedName, validatedUrl, dto.CollectionId, ownerId, dto.ParentFolderId);

        _context.Assets.Add(link);
        await _context.SaveChangesAsync(ct);
        return _assetMapper.ToDto(link);
    }

    public async Task<AssetResponseDto> UpdateAssetAsync(int id, UpdateAssetDto dto, string userId, CancellationToken ct = default)
    {
        var asset = await FindAssetWithAccessAsync(id, userId, CollectionRoles.Editor, ct);

        if (!string.IsNullOrEmpty(dto.FileName))
            asset.Rename(dto.FileName);
        if (dto.SortOrder.HasValue)
            asset.Reorder(dto.SortOrder.Value);
        if (dto.ClearGroup == true)
            asset.RemoveFromGroup();
        else if (dto.GroupId.HasValue)
            asset.AssignToGroup(dto.GroupId.Value);
        if (dto.ClearParentFolder == true)
            asset.MoveToFolder(null);
        else if (dto.ParentFolderId.HasValue)
            asset.MoveToFolder(dto.ParentFolderId.Value);

        await _context.SaveChangesAsync(ct);
        return _assetMapper.ToDto(asset);
    }

    public async Task<bool> DeleteAssetAsync(int id, string userId, CancellationToken ct = default)
    {
        var asset = await FindAssetWithAccessAsync(id, userId, CollectionRoles.Editor, ct);

        // Clean up physical file and thumbnails via helper
        await _cleanup.CleanupFilesAsync(asset, ct);

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
                asset.Reorder(i);
            }
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AssetResponseDto>> GetAssetsByGroupAsync(int groupId, string userId, CancellationToken ct = default)
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
        return _assetMapper.ToDtoList(result);
    }

    public async Task<AssetResponseDto> DuplicateAssetAsync(int id, int? targetFolderId, string userId, CancellationToken ct = default)
    {
        var source = await FindAssetWithAccessAsync(id, userId, CollectionRoles.Editor, ct);

        var clone = _assetFactory.Duplicate(source, userId, copySuffix: " (copy)", targetFolderId);

        _context.Assets.Add(clone);
        await _context.SaveChangesAsync(ct);
        await _notifier.NotifyAsync(userId, "AssetCreated", new { clone.Id, clone.FileName }, ct);
        return _assetMapper.ToDto(clone);
    }
}


