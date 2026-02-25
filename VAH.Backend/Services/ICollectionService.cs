using VAH.Backend.Models;

namespace VAH.Backend.Services;

public interface ICollectionService
{
    Task<List<Collection>> GetAllAsync();
    Task<Collection?> GetByIdAsync(int id);
    Task<CollectionWithItemsResult> GetWithItemsAsync(int id, int? folderId);
    Task<Collection> CreateAsync(Collection collection);
    Task<Collection> UpdateAsync(int id, Collection collection);
    Task<bool> DeleteAsync(int id);
}

/// <summary>
/// Result object for collection with items query.
/// </summary>
public class CollectionWithItemsResult
{
    public Collection Collection { get; set; } = null!;
    public List<Asset> Items { get; set; } = new();
    public List<Collection> SubCollections { get; set; } = new();
}
