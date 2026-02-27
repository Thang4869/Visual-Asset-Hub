using VAH.Backend.Models;

namespace VAH.Backend.Services;

public interface ICollectionService
{
    Task<List<Collection>> GetAllAsync(string userId);
    Task<Collection?> GetByIdAsync(int id, string userId);
    Task<CollectionWithItemsResult> GetWithItemsAsync(int id, int? folderId, string userId);
    Task<Collection> CreateAsync(Collection collection, string userId);
    Task<Collection> UpdateAsync(int id, Collection collection, string userId);
    Task<bool> DeleteAsync(int id, string userId);
}
