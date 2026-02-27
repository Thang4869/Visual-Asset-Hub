using VAH.Backend.Models;

namespace VAH.Backend.Services;

/// <summary>
/// Smart collection service — provides auto-categorized views of assets.
/// These are virtual/dynamic collections computed at query time.
/// </summary>
public interface ISmartCollectionService
{
    /// <summary>Get all available smart collection definitions for a user.</summary>
    Task<List<SmartCollectionDefinition>> GetDefinitionsAsync(string userId);

    /// <summary>Get items matching a smart collection's criteria.</summary>
    Task<PagedResult<Asset>> GetItemsAsync(string smartCollectionId, PaginationParams pagination, string userId);
}

/// <summary>
/// Describes a smart (dynamic) collection.
/// </summary>
public class SmartCollectionDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "📁";
    public string Color { get; set; } = "#2196F3";
    public int Count { get; set; }
}
