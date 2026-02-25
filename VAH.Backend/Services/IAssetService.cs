using VAH.Backend.Models;

namespace VAH.Backend.Services;

public interface IAssetService
{
    Task<PagedResult<Asset>> GetAssetsAsync(PaginationParams pagination);
    Task<Asset?> GetByIdAsync(int id);
    Task<Asset> CreateAssetAsync(Asset asset);
    Task<List<Asset>> UploadFilesAsync(List<IFormFile> files, int collectionId, int? folderId);
    Task<Asset> UpdatePositionAsync(int id, double positionX, double positionY);
    Task<Asset> CreateFolderAsync(CreateFolderDto dto);
    Task<Asset> CreateColorAsync(CreateColorDto dto);
    Task<Asset> CreateColorGroupAsync(CreateColorGroupDto dto);
    Task<Asset> CreateLinkAsync(CreateLinkDto dto);
    Task<Asset> UpdateAssetAsync(int id, UpdateAssetDto dto);
    Task<bool> DeleteAssetAsync(int id);
    Task ReorderAssetsAsync(List<int> assetIds);
    Task<List<Asset>> GetAssetsByGroupAsync(int groupId);
}
