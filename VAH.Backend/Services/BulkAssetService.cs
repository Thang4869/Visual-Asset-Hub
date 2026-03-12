using Microsoft.EntityFrameworkCore;
using VAH.Backend.Data;
using VAH.Backend.Models;

namespace VAH.Backend.Services;

/// <summary>
/// BulkAssetService — handles bulk delete, move, move-group, and tag operations.
/// SRP: Extracted from AssetService to keep each service focused.
/// </summary>
public class BulkAssetService : IBulkAssetService
{
    private readonly AppDbContext _context;
    private readonly AssetCleanupHelper _cleanup;
    private readonly INotificationService _notifier;
    private readonly ILogger<BulkAssetService> _logger;
    private readonly IPermissionService _permissions;

    public BulkAssetService(
        AppDbContext context,
        AssetCleanupHelper cleanup,
        INotificationService notifier,
        ILogger<BulkAssetService> logger,
        IPermissionService permissions)
    {
        _context = context;
        _cleanup = cleanup;
        _notifier = notifier;
        _logger = logger;
        _permissions = permissions;
    }

    /// <summary>Filter a list of assets to those the user can access (own or shared-collection permission).</summary>
    private async Task<List<Asset>> FilterByAccessAsync(List<Asset> assets, string userId, string minimumRole)
    {
        var result = new List<Asset>();
        foreach (var a in assets)
        {
            if (a.UserId == userId || await _permissions.HasPermissionAsync(a.CollectionId, userId, minimumRole))
                result.Add(a);
        }
        return result;
    }

    public async Task<int> BulkDeleteAsync(List<int> assetIds, string userId, CancellationToken ct = default)
    {
        if (assetIds == null || assetIds.Count == 0)
            throw new ArgumentException("Asset IDs are required.");

        var allAssets = await _context.Assets
            .Where(a => assetIds.Contains(a.Id))
            .ToListAsync(ct);
        var assets = await FilterByAccessAsync(allAssets, userId, CollectionRoles.Editor);

        foreach (var asset in assets)
        {
            ct.ThrowIfCancellationRequested();

            // Clean up physical files and thumbnails via helper
            await _cleanup.CleanupFilesAsync(asset);

            // Orphan prevention for folders
            if (asset.IsFolder)
            {
                var children = await _context.Assets
                    .Where(a => a.ParentFolderId == asset.Id)
                    .ToListAsync(ct);
                foreach (var child in children)
                    child.MoveToFolder(asset.ParentFolderId);
            }
        }

        _context.Assets.RemoveRange(assets);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Bulk deleted {Count} assets for user {UserId}", assets.Count, userId);
        await _notifier.NotifyAsync(userId, "AssetsBulkDeleted", new { count = assets.Count, assetIds }, ct);
        return assets.Count;
    }

    public async Task<int> BulkMoveAsync(BulkMoveDto dto, string userId, CancellationToken ct = default)
    {
        if (dto.AssetIds == null || dto.AssetIds.Count == 0)
            throw new ArgumentException("Asset IDs are required.");

        var allMoveAssets = await _context.Assets
            .Where(a => dto.AssetIds.Contains(a.Id))
            .ToListAsync(ct);
        var assets = await FilterByAccessAsync(allMoveAssets, userId, CollectionRoles.Editor);

        // Validate target collection exists if specified
        if (dto.TargetCollectionId.HasValue)
        {
            var coll = await _context.Collections.FindAsync([dto.TargetCollectionId.Value], ct);
            var collExists = coll != null && (coll.UserId == userId || coll.UserId == null
                || await _permissions.HasPermissionAsync(dto.TargetCollectionId.Value, userId, CollectionRoles.Editor));
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

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Bulk moved {Count} assets for user {UserId}", assets.Count, userId);
        await _notifier.NotifyAsync(userId, "AssetsBulkMoved", new { count = assets.Count }, ct);
        return assets.Count;
    }

    public async Task<int> BulkMoveGroupAsync(BulkMoveGroupDto dto, string userId, CancellationToken ct = default)
    {
        if (dto.AssetIds == null || dto.AssetIds.Count == 0)
            throw new ArgumentException("Asset IDs are required.");

        // Fetch the colors being moved
        var allMoveGroupAssets = await _context.Assets
            .Where(a => dto.AssetIds.Contains(a.Id))
            .ToListAsync(ct);
        var movedAssets = await FilterByAccessAsync(allMoveGroupAssets, userId, CollectionRoles.Editor);

        if (movedAssets.Count == 0) return 0;

        // Set group for each moved asset
        foreach (var asset in movedAssets)
        {
            if (dto.TargetGroupId.HasValue)
                asset.AssignToGroup(dto.TargetGroupId.Value);
            else
                asset.RemoveFromGroup();
        }

        // Get all existing colors in the target group (excluding the ones being moved)
        var targetGroupId = dto.TargetGroupId;
        var allExisting = await _context.Assets
            .Where(a => a.GroupId == targetGroupId
                        && a.ContentType == AssetContentType.Color
                        && !dto.AssetIds.Contains(a.Id))
            .OrderBy(a => a.SortOrder)
            .ToListAsync(ct);
        var existingInGroup = await FilterByAccessAsync(allExisting, userId, CollectionRoles.Editor);

        // Build final ordered list
        List<Asset> finalOrder;
        if (dto.InsertBeforeId.HasValue)
        {
            finalOrder = new List<Asset>();
            bool inserted = false;
            foreach (var existing in existingInGroup)
            {
                if (existing.Id == dto.InsertBeforeId.Value && !inserted)
                {
                    finalOrder.AddRange(movedAssets);
                    inserted = true;
                }
                finalOrder.Add(existing);
            }
            if (!inserted)
                finalOrder.AddRange(movedAssets); // append at end if insertBefore not found
        }
        else
        {
            finalOrder = new List<Asset>(existingInGroup);
            finalOrder.AddRange(movedAssets);
        }

        // Assign sort orders
        for (int i = 0; i < finalOrder.Count; i++)
        {
            finalOrder[i].Reorder(i);
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Bulk moved {Count} colors to group {GroupId} for user {UserId}",
            movedAssets.Count, dto.TargetGroupId, userId);
        await _notifier.NotifyAsync(userId, "AssetsBulkMoved", new { count = movedAssets.Count }, ct);
        return movedAssets.Count;
    }

    public async Task<int> BulkTagAsync(BulkTagDto dto, string userId, CancellationToken ct = default)
    {
        if (dto.AssetIds == null || dto.AssetIds.Count == 0)
            throw new ArgumentException("Asset IDs are required.");
        if (dto.TagIds == null || dto.TagIds.Count == 0)
            throw new ArgumentException("Tag IDs are required.");

        // Verify all assets the user can access
        var allTagAssets = await _context.Assets
            .Where(a => dto.AssetIds.Contains(a.Id))
            .ToListAsync(ct);
        var assets = await FilterByAccessAsync(allTagAssets, userId, CollectionRoles.Editor);

        // Verify tags belong to user
        var validTagIds = await _context.Tags
            .Where(t => dto.TagIds.Contains(t.Id) && t.UserId == userId)
            .Select(t => t.Id)
            .ToListAsync(ct);

        int count = 0;

        foreach (var asset in assets)
        {
            foreach (var tagId in validTagIds)
            {
                ct.ThrowIfCancellationRequested();

                if (dto.Remove)
                {
                    var junction = await _context.AssetTags
                        .FirstOrDefaultAsync(at => at.AssetId == asset.Id && at.TagId == tagId, ct);
                    if (junction != null)
                    {
                        _context.AssetTags.Remove(junction);
                        count++;
                    }
                }
                else
                {
                    var exists = await _context.AssetTags
                        .AnyAsync(at => at.AssetId == asset.Id && at.TagId == tagId, ct);
                    if (!exists)
                    {
                        _context.AssetTags.Add(new AssetTag { AssetId = asset.Id, TagId = tagId });
                        count++;
                    }
                }
            }
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Bulk tag operation ({Action}) on {Count} asset-tag pairs for user {UserId}",
            dto.Remove ? "remove" : "add", count, userId);
        return count;
    }
}
