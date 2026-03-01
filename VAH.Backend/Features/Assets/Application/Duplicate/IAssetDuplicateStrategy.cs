using System.Threading;
using System.Threading.Tasks;
using VAH.Backend.Models;

namespace VAH.Backend.Features.Assets.Application.Duplicate;

internal interface IAssetDuplicateStrategy
{
    bool CanHandle(int? targetFolderId);
    Task<AssetResponseDto> DuplicateAsync(int assetId, int? targetFolderId, CancellationToken cancellationToken = default);
}
