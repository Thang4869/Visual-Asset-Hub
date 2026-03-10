namespace VAH.Backend.Controllers;

/// <summary>
/// Machine-readable error code constants for the <c>"code"</c> extension in
/// <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/>.
/// Centralized to prevent typos and enable compile-time reference checking.
/// </summary>
/// <remarks>
/// All codes follow <c>snake_case</c> convention.
/// Add new codes here — never inline raw strings in controllers or services.
/// </remarks>
internal static class ErrorCodes
{
    public const string EmptyBatch = "empty_batch";
    public const string BatchSizeExceeded = "batch_size_exceeded";
    public const string InvalidSmartCollectionId = "invalid_smart_collection_id";
}
