using System.ComponentModel.DataAnnotations;

namespace VAH.Backend.Configuration;

/// <summary>
/// Configurable defaults for the Asset module.
/// Bound from <c>appsettings.json</c> section <see cref="SectionName"/>.
/// </summary>
public sealed class AssetOptions
{
    public const string SectionName = "AssetDefaults";

    /// <summary>Collection ID assigned to newly uploaded assets when none is specified.</summary>
    [Range(1, int.MaxValue)]
    public int DefaultCollectionId { get; init; } = 1;
}
