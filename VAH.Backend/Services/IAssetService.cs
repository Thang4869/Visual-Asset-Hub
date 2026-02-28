using VAH.Backend.Models;

namespace VAH.Backend.Services;

public interface IAssetService
{
    Task<PagedResult<AssetResponseDto>> GetAssetsAsync(PaginationParams pagination, string userId, CancellationToken ct = default);
    Task<AssetResponseDto> GetByIdAsync(int id, string userId, CancellationToken ct = default);
    Task<AssetResponseDto> CreateAssetAsync(CreateAssetDto dto, string userId, CancellationToken ct = default);
    Task<List<AssetResponseDto>> UploadFilesAsync(List<IFormFile> files, int collectionId, int? folderId, string userId, CancellationToken ct = default);
    Task<AssetResponseDto> UpdatePositionAsync(int id, double positionX, double positionY, string userId, CancellationToken ct = default);
    Task<AssetResponseDto> CreateFolderAsync(CreateFolderDto dto, string userId, CancellationToken ct = default);
    Task<AssetResponseDto> CreateColorAsync(CreateColorDto dto, string userId, CancellationToken ct = default);
    Task<AssetResponseDto> CreateColorGroupAsync(CreateColorGroupDto dto, string userId, CancellationToken ct = default);
    Task<AssetResponseDto> CreateLinkAsync(CreateLinkDto dto, string userId, CancellationToken ct = default);
    Task<AssetResponseDto> UpdateAssetAsync(int id, UpdateAssetDto dto, string userId, CancellationToken ct = default);
    Task<bool> DeleteAssetAsync(int id, string userId, CancellationToken ct = default);
    Task ReorderAssetsAsync(List<int> assetIds, string userId, CancellationToken ct = default);
    Task<List<AssetResponseDto>> GetAssetsByGroupAsync(int groupId, string userId, CancellationToken ct = default);
    Task<AssetResponseDto> DuplicateAssetAsync(int id, int? targetFolderId, string userId, CancellationToken ct = default);
}
