namespace VAH.Backend.Configuration;

/// <summary>
/// Holds adjustable defaults for assets to avoid hard-coded values in controllers.
/// </summary>
public sealed class AssetOptions
{
    public const string SectionName = "AssetDefaults";
    public int DefaultCollectionId { get; init; } = 1;
}
