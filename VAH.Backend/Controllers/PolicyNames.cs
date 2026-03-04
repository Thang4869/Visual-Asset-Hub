namespace VAH.Backend.Controllers;

/// <summary>
/// Centralized authorization policy name constants.
/// Eliminates magic strings across controllers — a single typo here is a compile-time fix,
/// not a silent runtime authorization failure.
/// </summary>
internal static class PolicyNames
{
    public const string RequireAssetRead = nameof(RequireAssetRead);
    public const string RequireAssetWrite = nameof(RequireAssetWrite);
}
