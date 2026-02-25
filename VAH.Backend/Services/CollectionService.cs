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

    public async Task<List<Collection>> GetAllAsync()
    {
        return await _context.Collections
            .OrderBy(c => c.Order)
            .ToListAsync();
    }

    public async Task<Collection?> GetByIdAsync(int id)
    {
        return await _context.Collections.FindAsync(id);
    }

    public async Task<CollectionWithItemsResult> GetWithItemsAsync(int id, int? folderId)
    {
        var collection = await _context.Collections.FindAsync(id)
            ?? throw new KeyNotFoundException($"Collection {id} not found.");

        var items = await _context.Assets
            .Where(a => a.CollectionId == id && a.ParentFolderId == folderId)
            .OrderBy(a => a.IsFolder ? 0 : 1)
            .ThenBy(a => a.SortOrder)
            .ThenBy(a => a.FileName)
            .ToListAsync();

        var subcollections = await _context.Collections
            .Where(c => c.ParentId == id)
            .OrderBy(c => c.Order)
            .ToListAsync();

        return new CollectionWithItemsResult
        {
            Collection = collection,
            Items = items,
            SubCollections = subcollections
        };
    }

    public async Task<Collection> CreateAsync(Collection collection)
    {
        if (string.IsNullOrWhiteSpace(collection.Name))
            throw new ArgumentException("Collection name is required.");

        collection.Name = collection.Name.Trim();
        collection.CreatedAt = DateTime.UtcNow;

        _context.Collections.Add(collection);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Collection created: {Name} (Id={Id})", collection.Name, collection.Id);
        return collection;
    }

    public async Task<Collection> UpdateAsync(int id, Collection collection)
    {
        if (id != collection.Id)
            throw new ArgumentException("ID mismatch.");

        var existing = await _context.Collections.FindAsync(id)
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

    public async Task<bool> DeleteAsync(int id)
    {
        var collection = await _context.Collections.FindAsync(id)
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

        _logger.LogInformation("Collection deleted: {Name} (Id={Id})", collection.Name, id);
        return true;
    }
}
