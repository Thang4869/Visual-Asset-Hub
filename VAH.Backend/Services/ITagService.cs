using VAH.Backend.Models;

namespace VAH.Backend.Services;

public interface ITagService
{
    Task<List<Tag>> GetAllAsync(string userId, CancellationToken ct = default);
    Task<Tag> GetByIdAsync(int id, string userId, CancellationToken ct = default);
    Task<Tag> CreateAsync(CreateTagDto dto, string userId, CancellationToken ct = default);
    /// <summary>Create a tag or return existing. Tuple: (tag, wasCreated).</summary>
    Task<(Tag Tag, bool Created)> CreateOrGetAsync(CreateTagDto dto, string userId, CancellationToken ct = default);
    Task<Tag> UpdateAsync(int id, UpdateTagDto dto, string userId, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, string userId, CancellationToken ct = default);
    Task<List<Tag>> GetOrCreateTagsAsync(IEnumerable<string> tagNames, string userId, CancellationToken ct = default);
    Task SetAssetTagsAsync(int assetId, List<int> tagIds, string userId, CancellationToken ct = default);
    Task AddAssetTagsAsync(int assetId, List<int> tagIds, string userId, CancellationToken ct = default);
    Task RemoveAssetTagsAsync(int assetId, List<int> tagIds, string userId, CancellationToken ct = default);
    Task<List<Tag>> GetAssetTagsAsync(int assetId, string userId, CancellationToken ct = default);
    Task MigrateCommaSeparatedTagsAsync(string userId, CancellationToken ct = default);
}
