using VAH.Backend.Models;

namespace VAH.Backend.Services;

public interface ITagService
{
    Task<List<Tag>> GetAllAsync(string userId);
    Task<Tag> GetByIdAsync(int id, string userId);
    Task<Tag> CreateAsync(CreateTagDto dto, string userId);
    Task<Tag> UpdateAsync(int id, UpdateTagDto dto, string userId);
    Task<bool> DeleteAsync(int id, string userId);
    Task<List<Tag>> GetOrCreateTagsAsync(IEnumerable<string> tagNames, string userId);
    Task SetAssetTagsAsync(int assetId, List<int> tagIds, string userId);
    Task AddAssetTagsAsync(int assetId, List<int> tagIds, string userId);
    Task RemoveAssetTagsAsync(int assetId, List<int> tagIds, string userId);
    Task<List<Tag>> GetAssetTagsAsync(int assetId, string userId);
    Task MigrateCommaSeparatedTagsAsync(string userId);
}
