using VAH.Backend.Models;

namespace VAH.Backend.Services;

public interface ICollectionService
{
    Task<List<Collection>> GetAllAsync(string userId, CancellationToken ct = default);
    Task<Collection?> GetByIdAsync(int id, string userId, CancellationToken ct = default);
    Task<CollectionWithItemsResult> GetWithItemsAsync(int id, int? folderId, string userId, CancellationToken ct = default);
    Task<Collection> CreateAsync(CreateCollectionDto dto, string userId, CancellationToken ct = default);
    Task<Collection> UpdateAsync(int id, UpdateCollectionDto dto, string userId, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, string userId, CancellationToken ct = default);
}
