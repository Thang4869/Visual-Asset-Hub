using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VAH.Backend.Models;

namespace VAH.Backend.Features.Assets.Application;

public interface IAssetApplicationService
{
    Task<PagedResult<AssetResponseDto>> GetAssetsAsync(PaginationParams pagination, CancellationToken ct = default);
    Task<AssetResponseDto> GetAssetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<AssetResponseDto>> GetAssetsByGroupAsync(int groupId, CancellationToken ct = default);
    Task<AssetResponseDto> CreateAssetAsync(CreateAssetDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<AssetResponseDto>> UploadFilesAsync(IReadOnlyCollection<UploadedFileDto> files, int? collectionId, int? folderId, CancellationToken ct = default);
    Task<AssetResponseDto> UpdateAssetAsync(int id, UpdateAssetDto dto, CancellationToken ct = default);
    Task DeleteAssetAsync(int id, CancellationToken ct = default);
    Task<AssetResponseDto> DuplicateAssetAsync(int id, int? targetFolderId, CancellationToken ct = default);
}
