using VAH.Backend.Models;

namespace VAH.Backend.Services;

public interface IAssetService
{
    Task<PagedResult<Asset>> GetAssetsAsync(PaginationParams pagination, string userId, CancellationToken ct = default);
    Task<Asset> GetByIdAsync(int id, string userId, CancellationToken ct = default);
    Task<Asset> CreateAssetAsync(CreateAssetDto dto, string userId, CancellationToken ct = default);
    Task<List<Asset>> UploadFilesAsync(List<IFormFile> files, int collectionId, int? folderId, string userId, CancellationToken ct = default);
    Task<Asset> UpdatePositionAsync(int id, double positionX, double positionY, string userId, CancellationToken ct = default);
    Task<Asset> CreateFolderAsync(CreateFolderDto dto, string userId, CancellationToken ct = default);
    Task<Asset> CreateColorAsync(CreateColorDto dto, string userId, CancellationToken ct = default);
    Task<Asset> CreateColorGroupAsync(CreateColorGroupDto dto, string userId, CancellationToken ct = default);
    Task<Asset> CreateLinkAsync(CreateLinkDto dto, string userId, CancellationToken ct = default);
    Task<Asset> UpdateAssetAsync(int id, UpdateAssetDto dto, string userId, CancellationToken ct = default);
    Task<bool> DeleteAssetAsync(int id, string userId, CancellationToken ct = default);
    Task ReorderAssetsAsync(List<int> assetIds, string userId, CancellationToken ct = default);
    Task<List<Asset>> GetAssetsByGroupAsync(int groupId, string userId, CancellationToken ct = default);
    Task<Asset> DuplicateAssetAsync(int id, int? targetFolderId, string userId, CancellationToken ct = default);
}
