namespace VAH.Backend.Controllers;

/// <summary>
/// Centralized limits for bulk/batch operations.
/// Prevents unbounded queries that could degrade database performance.
/// </summary>
internal static class BulkOperationLimits
{
    /// <summary>Maximum number of items in a single bulk request (delete, move, tag, reorder).</summary>
    public const int MaxBatchSize = 500;
}
