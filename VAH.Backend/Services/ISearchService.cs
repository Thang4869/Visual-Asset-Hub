using VAH.Backend.Models;

namespace VAH.Backend.Services;

/// <summary>
/// Search service — handles search queries across assets and collections.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Search assets and collections by name/tags with filtering and pagination.
    /// </summary>
    Task<SearchResult> SearchAsync(string userId, string? query, string? type, int? collectionId, int page = 1, int pageSize = 50, CancellationToken ct = default);
}
