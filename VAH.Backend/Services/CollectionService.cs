using Microsoft.EntityFrameworkCore;
using VAH.Backend.Data;
using VAH.Backend.Models;

namespace VAH.Backend.Services;

public class CollectionService : ICollectionService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CollectionService> _logger;

    public CollectionService(AppDbContext context, ILogger<CollectionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Collection>> GetAllAsync(string userId)
    {
        // Return system collections (UserId == null) + user's own collections
        return await _context.Collections
            .Where(c => c.UserId == userId || c.UserId == null)
            .OrderBy(c => c.Order)
            .ToListAsync();
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
        return true;
    }
}
