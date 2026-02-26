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

    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
        SlidingExpiration = TimeSpan.FromMinutes(2)
    };

    public CollectionService(AppDbContext context, ILogger<CollectionService> logger, IDistributedCache cache)
    {
        _context = context;
        _logger = logger;
        _cache = cache;
    }

    private string CacheKey(string userId) => $"collections:all:{userId}";

    /// <summary>Invalidate user's collection list cache after mutation.</summary>
    private async Task InvalidateCacheAsync(string userId)
    {
        try
        {
            await _cache.RemoveAsync(CacheKey(userId));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate cache for user {UserId}", userId);
        }
    }

    public async Task<List<Collection>> GetAllAsync(string userId)
    {
        // Try cache first
        try
        {
            var cached = await _cache.GetStringAsync(CacheKey(userId));
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

        // Return system collections (UserId == null) + user's own collections
        var collections = await _context.Collections
            .Where(c => c.UserId == userId || c.UserId == null)
            .OrderBy(c => c.Order)
            .ToListAsync();

        // Write to cache (fire-and-forget style with try-catch)
        try
        {
            var json = JsonSerializer.Serialize(collections);
            await _cache.SetStringAsync(CacheKey(userId), json, CacheOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache write failed");
        }

        return collections;
    }

    public async Task<Collection?> GetByIdAsync(int id, string userId)
    {
        return await _context.Collections
            .FirstOrDefaultAsync(c => c.Id == id && (c.UserId == userId || c.UserId == null));
    }

    public async Task<CollectionWithItemsResult> GetWithItemsAsync(int id, int? folderId, string userId)
    {
        var collection = await _context.Collections
            .FirstOrDefaultAsync(c => c.Id == id && (c.UserId == userId || c.UserId == null))
            ?? throw new KeyNotFoundException($"Collection {id} not found.");

        // Only show user's own assets
        var items = await _context.Assets
            .Where(a => a.CollectionId == id && a.ParentFolderId == folderId && a.UserId == userId)
            .OrderBy(a => a.IsFolder ? 0 : 1)
            .ThenBy(a => a.SortOrder)
            .ThenBy(a => a.FileName)
            .ToListAsync();

        // Show system subcollections + user's own
        var subcollections = await _context.Collections
            .Where(c => c.ParentId == id && (c.UserId == userId || c.UserId == null))
            .OrderBy(c => c.Order)
            .ToListAsync();

        return new CollectionWithItemsResult
        {
            Collection = collection,
            Items = items,
            SubCollections = subcollections
        };
    }

    public async Task<Collection> CreateAsync(Collection collection, string userId)
    {
        if (string.IsNullOrWhiteSpace(collection.Name))
            throw new ArgumentException("Collection name is required.");

        collection.Name = collection.Name.Trim();
        collection.CreatedAt = DateTime.UtcNow;
        collection.UserId = userId;

        _context.Collections.Add(collection);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Collection created: {Name} (Id={Id}) by user {UserId}", collection.Name, collection.Id, userId);
        await InvalidateCacheAsync(userId);
        return collection;
    }

    public async Task<Collection> UpdateAsync(int id, Collection collection, string userId)
    {
        if (id != collection.Id)
            throw new ArgumentException("ID mismatch.");

        // Only allow updating own collections (not system ones)
        var existing = await _context.Collections
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId)
            ?? throw new KeyNotFoundException($"Collection {id} not found.");

        existing.Name = collection.Name?.Trim() ?? existing.Name;
        existing.Description = collection.Description ?? existing.Description;
        existing.Color = collection.Color ?? existing.Color;
        existing.Type = collection.Type ?? existing.Type;
        existing.Order = collection.Order;
        existing.LayoutType = collection.LayoutType ?? existing.LayoutType;

        await _context.SaveChangesAsync();
        await InvalidateCacheAsync(userId);
        return existing;
    }

    public async Task<bool> DeleteAsync(int id, string userId)
    {
        // Only allow deleting own collections
        var collection = await _context.Collections
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId)
            ?? throw new KeyNotFoundException($"Collection {id} not found.");

        // Move child collections to top-level instead of deleting them
        var children = await _context.Collections
            .Where(c => c.ParentId == id)
            .ToListAsync();

        foreach (var child in children)
        {
            child.ParentId = null;
        }

        _context.Collections.Remove(collection);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Collection deleted: {Name} (Id={Id}) by user {UserId}", collection.Name, id, userId);
        await InvalidateCacheAsync(userId);
        return true;
    }
}
