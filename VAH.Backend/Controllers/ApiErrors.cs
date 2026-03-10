using Microsoft.AspNetCore.Mvc;

namespace VAH.Backend.Controllers;

/// <summary>
/// Factory methods for standardized <see cref="ProblemDetails"/> error responses.
/// Ensures every controller returns the same schema and machine-readable error codes.
/// </summary>
/// <remarks>
/// <para>Error codes follow <c>snake_case</c> convention and are placed in
/// <see cref="ProblemDetails.Extensions"/> under key <see cref="CodeKey"/>.</para>
/// <para><c>Type</c> URIs use a stable URN scheme (<c>urn:vah:error:{code}</c>) for
/// deterministic monitoring, alerting, and API contract tracking per RFC 9457 §3.1.1.</para>
/// <para><b>Extensions schema</b> — only two keys are allowed:
/// <c>code</c> (string, always present) and <c>meta</c> (object, optional context data).
/// This keeps the contract predictable for clients.</para>
/// <para><c>Instance</c> and <c>traceId</c> are NOT set here — they are enriched
/// globally by <see cref="Middleware.GlobalExceptionHandler"/> so every ProblemDetails
/// (including model-validation and unhandled exceptions) gets them consistently.</para>
/// </remarks>
internal static class ApiErrors
{
    private const string ErrorTypeBase = "urn:vah:error:";
    private const string CodeKey = "code";
    private const string MetaKey = "meta";
    private const int MaxInputEchoLength = 100;

    /// <summary>Batch request body has zero items.</summary>
    public static ProblemDetails EmptyBatch() => new()
    {
        Type = $"{ErrorTypeBase}{ErrorCodes.EmptyBatch}",
        Title = "AssetIds must not be empty.",
        Detail = "The request body must contain at least one asset ID.",
        Status = StatusCodes.Status400BadRequest,
        Extensions = { [CodeKey] = ErrorCodes.EmptyBatch }
    };

    /// <summary>Batch request exceeds <see cref="BulkOperationLimits.MaxBatchSize"/>.</summary>
    public static ProblemDetails BatchSizeExceeded() => new()
    {
        Type = $"{ErrorTypeBase}{ErrorCodes.BatchSizeExceeded}",
        Title = $"Batch size exceeds the maximum of {BulkOperationLimits.MaxBatchSize}.",
        Detail = $"Reduce the number of items to at most {BulkOperationLimits.MaxBatchSize} per request.",
        Status = StatusCodes.Status400BadRequest,
        Extensions = { [CodeKey] = ErrorCodes.BatchSizeExceeded }
    };

    /// <summary>Smart-collection identifier is not a recognised definition key.</summary>
    public static ProblemDetails InvalidSmartCollectionId(string id) => new()
    {
        Type = $"{ErrorTypeBase}{ErrorCodes.InvalidSmartCollectionId}",
        Title = "Unknown smart collection identifier.",
        Detail = "The provided identifier does not match any registered smart collection definition.",
        Status = StatusCodes.Status400BadRequest,
        Extensions =
        {
            [CodeKey] = ErrorCodes.InvalidSmartCollectionId,
            [MetaKey] = new { invalidId = Truncate(id) }
        }
    };

    /// <summary>Normalize and truncate user-supplied input to a safe length before echoing in responses.</summary>
    private static string Truncate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var trimmed = value.Trim();
        return trimmed.Length <= MaxInputEchoLength ? trimmed : string.Concat(trimmed.AsSpan(0, MaxInputEchoLength), "…");
    }
}
