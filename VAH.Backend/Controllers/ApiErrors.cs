using Microsoft.AspNetCore.Mvc;

namespace VAH.Backend.Controllers;

/// <summary>
/// Factory methods for standardized <see cref="ProblemDetails"/> error responses.
/// Ensures every controller returns the same schema and machine-readable error codes.
/// </summary>
/// <remarks>
/// Error codes follow the pattern <c>snake_case</c> and are placed in the
/// <see cref="ProblemDetails.Extensions"/> dictionary under key <c>"code"</c>.
/// </remarks>
internal static class ApiErrors
{
    /// <summary>Batch request body has zero items.</summary>
    public static ProblemDetails EmptyBatch() => new()
    {
        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        Title = "AssetIds must not be empty.",
        Status = StatusCodes.Status400BadRequest,
        Extensions = { ["code"] = "empty_batch" }
    };

    /// <summary>Batch request exceeds <see cref="BulkOperationLimits.MaxBatchSize"/>.</summary>
    public static ProblemDetails BatchSizeExceeded() => new()
    {
        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        Title = $"Batch size exceeds the maximum of {BulkOperationLimits.MaxBatchSize}.",
        Status = StatusCodes.Status400BadRequest,
        Extensions = { ["code"] = "batch_size_exceeded" }
    };

    /// <summary>Smart-collection identifier is not a recognised definition key.</summary>
    public static ProblemDetails InvalidSmartCollectionId(string id) => new()
    {
        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        Title = $"Unknown smart collection identifier '{id}'.",
        Status = StatusCodes.Status400BadRequest,
        Extensions = { ["code"] = "invalid_smart_collection_id" }
    };
}
