namespace VAH.Backend.Controllers;

/// <summary>
/// Centralized rate-limiting policy name constants.
/// Must match registrations in <see cref="Extensions.ServiceCollectionExtensions.AddRateLimitingPolicies"/>.
/// </summary>
internal static class RateLimitPolicies
{
    public const string Fixed = nameof(Fixed);
    public const string Upload = nameof(Upload);
    public const string Search = nameof(Search);
}
