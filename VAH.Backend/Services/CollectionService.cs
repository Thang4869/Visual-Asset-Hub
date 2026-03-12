using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using VAH.Backend.Data;
using VAH.Backend.Models;

namespace VAH.Backend.Services;

public class CollectionService : ICollectionService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CollectionService> _logger;
    private readonly IDistributedCache _cache;
    private readonly INotificationService _notifier;
    private readonly IPermissionService _permissionService;

    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
        SlidingExpiration = TimeSpan.FromMinutes(2)
    };

    public CollectionService(AppDbContext context, ILogger<CollectionService> logger, IDistributedCache cache, INotificationService notifier, IPermissionService permissionService)
    {
        _context = context;
        _logger = logger;
        _cache = cache;
        _notifier = notifier;
        _permissionService = permissionService;
    }

    private string CacheKey(string userId) => $"collections:all:{userId}";

    /// <summary>Invalidate user's collection list cache after mutation.</summary>
    private async Task InvalidateCacheAsync(string userId, CancellationToken ct)
    {
        try
        {
            await _cache.RemoveAsync(CacheKey(userId), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate cache for user {UserId}", userId);
        }
    }

    public async Task<List<Collection>> GetAllAsync(string userId, CancellationToken ct = default)
    {
        // Try cache first
        try
        {
            var cached = await _cache.GetStringAsync(CacheKey(userId), ct);
            if (cached != null)
            {
                _logger.LogDebug("Cache hit for collections list (user {UserId})", userId);
                return JsonSerializer.Deserialize<List<Collection>>(cached) ?? new();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache read failed, falling back to DB");
        }

        // Return system collections (UserId == null) + user's own collections + shared collections
        var collections = await _context.Collections
            .Where(c => c.UserId == userId || c.UserId == null)
            .OrderBy(c => c.Order)
            .ToListAsync(ct);

        // Add shared collections
        var sharedCollections = await _permissionService.GetSharedCollectionsAsync(userId, ct);
        var existingIds = collections.Select(c => c.Id).ToHashSet();
        foreach (var sc in sharedCollections)
        {
            if (!existingIds.Contains(sc.Id))
                collections.Add(sc);
        }

        // Write to cache (fire-and-forget style with try-catch)
        try
        {
            var json = JsonSerializer.Serialize(collections);
            await _cache.SetStringAsync(CacheKey(userId), json, CacheOptions, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache write failed");
        }

        return collections;
    }

    public async Task<Collection?> GetByIdAsync(int id, string userId, CancellationToken ct = default)
    {
        var collection = await _context.Collections
            .FirstOrDefaultAsync(c => c.Id == id, ct);
        if (collection == null) return null;

        // Allow access if: system collection, own collection, or has permission
        if (collection.UserId == null || collection.UserId == userId)
            return collection;

        var hasAccess = await _permissionService.HasPermissionAsync(id, userId, CollectionRoles.Viewer, ct);
        return hasAccess ? collection : null;
    }

    public async Task<CollectionWithItemsResult> GetWithItemsAsync(int id, int? folderId, string userId, CancellationToken ct = default)
    {
        var collection = await _context.Collections.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Collection {id} not found.");

        // Check access: system, own, or shared
        bool isOwnerOrSystem = collection.UserId == null || collection.UserId == userId;
        if (!isOwnerOrSystem)
        {
            var hasAccess = await _permissionService.HasPermissionAsync(id, userId, CollectionRoles.Viewer, ct);
            if (!hasAccess) throw new KeyNotFoundException($"Collection {id} not found.");
        }

        // For shared collections, show the owner's assets
        var assetOwner = collection.UserId ?? userId;
        var items = await _context.Assets
            .Where(a => a.CollectionId == id && a.ParentFolderId == folderId && a.UserId == assetOwner)
            .OrderBy(a => a.IsFolder ? 0 : 1)
            .ThenBy(a => a.SortOrder)
            .ThenBy(a => a.FileName)
            .ToListAsync(ct);

        // Show system subcollections + user's own
        var subcollections = await _context.Collections
            .Where(c => c.ParentId == id && (c.UserId == userId || c.UserId == null))
            .OrderBy(c => c.Order)
            .ToListAsync(ct);

        return new CollectionWithItemsResult
        {
            Collection = collection,
            Items = AssetMapper.ToDtoList(items),
            SubCollections = subcollections
        };
    }

    public async Task<Collection> CreateAsync(CreateCollectionDto dto, string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Collection name is required.");

        var collection = new Collection
        {
            Name = dto.Name.Trim(),
            Description = dto.Description ?? string.Empty,
            ParentId = dto.ParentId,
            Color = dto.Color ?? "#007bff",
            Type = dto.Type ?? CollectionType.Default,
            LayoutType = dto.LayoutType ?? LayoutType.Grid,
            CreatedAt = DateTime.UtcNow,
            UserId = userId
        };

        _context.Collections.Add(collection);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Collection created: {Name} (Id={Id}) by user {UserId}", collection.Name, collection.Id, userId);
        await InvalidateCacheAsync(userId, ct);
        await _notifier.NotifyAsync(userId, "CollectionCreated", new { collection.Id, collection.Name }, ct);
        return collection;
    }

    public async Task<Collection> UpdateAsync(int id, UpdateCollectionDto dto, string userId, CancellationToken ct = default)
    {
        // Allow updating own collections or shared with editor/owner role
        var existing = await _context.Collections.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Collection {id} not found.");

        bool isOwner = existing.IsOwnedBy(userId);
        if (!isOwner)
        {
            var canWrite = await _permissionService.HasPermissionAsync(id, userId, CollectionRoles.Editor, ct);
            if (!canWrite) throw new KeyNotFoundException($"Collection {id} not found.");
        }

        existing.ApplyUpdate(dto);

        await _context.SaveChangesAsync(ct);
        await InvalidateCacheAsync(userId, ct);
        await _notifier.NotifyAsync(userId, "CollectionUpdated", new { existing.Id, existing.Name }, ct);
        return existing;
    }

    public async Task<bool> DeleteAsync(int id, string userId, CancellationToken ct = default)
    {
        // Only allow deleting own collections
        var collection = await _context.Collections
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct)
            ?? throw new KeyNotFoundException($"Collection {id} not found.");

        // Move child collections to top-level instead of deleting them
        var children = await _context.Collections
            .Where(c => c.ParentId == id)
            .ToListAsync(ct);

        foreach (var child in children)
        {
            child.ParentId = null;
        }

        _context.Collections.Remove(collection);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Collection deleted: {Name} (Id={Id}) by user {UserId}", collection.Name, id, userId);
        await InvalidateCacheAsync(userId, ct);
        await _notifier.NotifyAsync(userId, "CollectionDeleted", new { id }, ct);
        return true;
    }
}
