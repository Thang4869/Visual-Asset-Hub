using VAH.Backend.Models;

namespace VAH.Backend.Services;

public interface IAssetService
{
    Task<PagedResult<Asset>> GetAssetsAsync(PaginationParams pagination, string userId);
    Task<Asset?> GetByIdAsync(int id, string userId);
    Task<Asset> CreateAssetAsync(Asset asset, string userId);
    Task<List<Asset>> UploadFilesAsync(List<IFormFile> files, int collectionId, int? folderId, string userId);
    Task<Asset> UpdatePositionAsync(int id, double positionX, double positionY, string userId);
    Task<Asset> CreateFolderAsync(CreateFolderDto dto, string userId);
    Task<Asset> CreateColorAsync(CreateColorDto dto, string userId);
    Task<Asset> CreateColorGroupAsync(CreateColorGroupDto dto, string userId);
    Task<Asset> CreateLinkAsync(CreateLinkDto dto, string userId);
    Task<Asset> UpdateAssetAsync(int id, UpdateAssetDto dto, string userId);
    Task<bool> DeleteAssetAsync(int id, string userId);
    Task ReorderAssetsAsync(List<int> assetIds, string userId);
    Task<List<Asset>> GetAssetsByGroupAsync(int groupId, string userId);
}
