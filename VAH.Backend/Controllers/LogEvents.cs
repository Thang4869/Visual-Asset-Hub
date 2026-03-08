namespace VAH.Backend.Controllers;

/// <summary>
/// Structured log event IDs for controller-layer operations.
/// Event IDs make log filtering, alerting, and dashboards deterministic.
/// </summary>
/// <remarks>
/// Ranges: 1xxx = Auth, 2xxx = Bulk, 3xxx = Collection, 4xxx = Asset-layout,
/// 5xxx = Tags, 6xxx = Permissions, 7xxx = Search, 8xxx = Health.
/// </remarks>
internal static class LogEvents
{
    // ── Auth ──
    public static readonly EventId RegisterAttempt = new(1001, "RegisterAttempt");
    public static readonly EventId LoginAttempt = new(1002, "LoginAttempt");

    // ── Bulk operations ──
    public static readonly EventId BulkDelete = new(2001, "BulkDelete");
    public static readonly EventId BulkMove = new(2002, "BulkMove");
    public static readonly EventId BulkMoveGroup = new(2003, "BulkMoveGroup");
    public static readonly EventId BulkTag = new(2004, "BulkTag");

    // ── Collections ──
    public static readonly EventId CollectionCreated = new(3001, "CollectionCreated");
    public static readonly EventId CollectionDeleted = new(3002, "CollectionDeleted");

    // ── Asset layout ──
    public static readonly EventId Reorder = new(4001, "Reorder");

    // ── Tags ──
    public static readonly EventId TagCreated = new(5001, "TagCreated");
    public static readonly EventId TagDeleted = new(5002, "TagDeleted");
    public static readonly EventId TagMigration = new(5003, "TagMigration");

    // ── Permissions ──
    public static readonly EventId PermissionGranted = new(6001, "PermissionGranted");
    public static readonly EventId PermissionUpdated = new(6002, "PermissionUpdated");
    public static readonly EventId PermissionRevoked = new(6003, "PermissionRevoked");

    // ── Asset creation ──
    public static readonly EventId AssetCreated = new(4101, "AssetCreated");
}
