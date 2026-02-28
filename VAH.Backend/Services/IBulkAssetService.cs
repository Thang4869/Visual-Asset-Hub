using VAH.Backend.Models;

namespace VAH.Backend.Services;

/// <summary>
/// Handles bulk operations on assets (delete, move, tag).
/// ISP: Separated from IAssetService so consumers needing only bulk ops
/// don't depend on the full single-asset interface.
/// </summary>
public interface IBulkAssetService
{
    Task<int> BulkDeleteAsync(List<int> assetIds, string userId, CancellationToken ct = default);
    Task<int> BulkMoveAsync(BulkMoveDto dto, string userId, CancellationToken ct = default);
    Task<int> BulkMoveGroupAsync(BulkMoveGroupDto dto, string userId, CancellationToken ct = default);
    Task<int> BulkTagAsync(BulkTagDto dto, string userId, CancellationToken ct = default);
}
