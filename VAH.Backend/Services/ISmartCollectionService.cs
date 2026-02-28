using VAH.Backend.Models;

namespace VAH.Backend.Services;

/// <summary>
/// Smart collection service — provides auto-categorized views of assets.
/// These are virtual/dynamic collections computed at query time.
/// </summary>
public interface ISmartCollectionService
{
    /// <summary>Get all available smart collection definitions for a user.</summary>
    Task<List<SmartCollectionDefinition>> GetDefinitionsAsync(string userId, CancellationToken ct = default);

    /// <summary>Get items matching a smart collection's criteria.</summary>
    Task<PagedResult<AssetResponseDto>> GetItemsAsync(string smartCollectionId, PaginationParams pagination, string userId, CancellationToken ct = default);
}
