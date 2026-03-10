namespace VAH.Backend.Controllers;

/// <summary>
/// Centralized limits for bulk/batch operations.
/// Prevents unbounded queries that could degrade database performance.
/// </summary>
/// <remarks>
/// <para>500 is chosen as a safe upper bound that balances UX (most users select &lt;200 items)
/// against DB pressure (EF Core parameterized IN clause + per-row storage I/O).</para>
/// <para>If per-environment tuning is needed, promote to <c>IOptions&lt;BulkOptions&gt;</c>
/// and inject via configuration.</para>
/// </remarks>
internal static class BulkOperationLimits
{
    /// <summary>Maximum number of items in a single bulk request (delete, move, tag, reorder).</summary>
    public const int MaxBatchSize = 500;
}
