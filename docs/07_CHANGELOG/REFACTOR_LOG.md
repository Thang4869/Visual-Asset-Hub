# REFACTOR LOG

> **Last Updated**: 2026-03-07

Tracks completed refactoring efforts with before/after comparisons.

---

## RF-007 — Batch Guards, Search Rate Limiting & Liveness Probe

**Date**: 2026-03-07
**Scope**: Bulk/layout controllers, SearchController, HealthController, TagService, ServiceCollectionExtensions
**Branch**: `refactor/rate-limit-batch-limits`

### Summary

Added batch-size ceilings to all bulk/reorder endpoints, introduced a dedicated search rate-limit policy, split health probes into readiness + liveness, and made tag creation idempotent with correct HTTP semantics. 17 files changed (167 insertions, 27 deletions).

### Key Changes

#### 1. Batch Size Guard via BulkOperationLimits

**Before**
```csharp
// Only empty-check existed
if (dto.AssetIds is not { Count: > 0 })
    return BadRequest(...);
// No upper bound — client could send 10,000 IDs
```

**After**
```csharp
internal static class BulkOperationLimits
{
    public const int MaxBatchSize = 500;
}

// Every bulk + reorder endpoint:
if (dto.AssetIds is not { Count: > 0 })
    return BadRequest(new ProblemDetails { Title = "AssetIds must not be empty.", Status = 400 });

if (dto.AssetIds.Count > BulkOperationLimits.MaxBatchSize)
    return BadRequest(new ProblemDetails
    {
        Title = $"Batch size exceeds the maximum of {BulkOperationLimits.MaxBatchSize}.",
        Status = 400
    });
```

Applied to: `BulkDelete`, `BulkMove`, `BulkMoveGroup`, `BulkTag`, `ReorderAssets` (5 endpoints).

**Impact**: Prevents unbounded queries; single constant to adjust site-wide.

#### 2. Search Sliding-Window Rate Limiter

**Before**
```csharp
// SearchController had no rate limiting
[Route("api/v1/[controller]")]
[Authorize]
public sealed class SearchController ...
```

**After**
```csharp
options.AddSlidingWindowLimiter("Search", opt =>
{
    opt.PermitLimit = 60;
    opt.Window = TimeSpan.FromMinutes(1);
    opt.SegmentsPerWindow = 6;   // 10-second segments
    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    opt.QueueLimit = 5;
});

[EnableRateLimiting(RateLimitPolicies.Search)]
public sealed class SearchController ...
```

**Impact**: Protects against search abuse; sliding window is smoother than fixed window.

#### 3. Liveness Probe (HealthController)

**Before**
```csharp
public sealed class HealthController(...) : BaseApiController
{
    // Single combined health check
    [HttpGet] public async Task<IActionResult> GetHealth(...)
}
```

**After**
```csharp
public sealed class HealthController(...) : ControllerBase  // not BaseApiController
{
    [HttpGet]       // Readiness — probes DB + storage
    public async Task<IActionResult> GetHealth(...)

    [HttpGet("live")]  // Liveness — process-only, no deps
    public IActionResult GetLiveness()
        => Ok(new LivenessResult("alive", DateTime.UtcNow));
}
```

**Impact**: K8s livenessProbe can use `/health/live` without triggering DB connections. HealthController no longer inherits auth-related base responses.

#### 4. Idempotent Tag Creation

**Before**
```csharp
public async Task<Tag> CreateAsync(CreateTagDto dto, string userId, ...)
{
    // ... finds existing
    if (existing != null) return existing;  // Always 201
    // ... creates new
    return tag;  // Always 201
}
```

**After**
```csharp
public async Task<(Tag Tag, bool Created)> CreateOrGetAsync(CreateTagDto dto, ...)
{
    if (existing != null) return (existing, false);   // → 200 OK
    // ...
    return (tag, true);  // → 201 Created
}

// Controller:
var (tag, created) = await tagService.CreateOrGetAsync(dto, userId, ct);
return created
    ? CreatedAtAction(nameof(GetTag), new { id = tag.Id }, tag)
    : Ok(tag);
```

**Impact**: Correct HTTP semantics — `201` only for actual creation; `200` for existing. Frontend can distinguish new vs existing by status code.

---

## RF-006 — ProblemDetails Consistency, Input Validation & SRP Extraction

**Date**: 2026-03-06
**Scope**: BaseApiController, BulkAssetsController, PermissionsController, TagsController, SharedCollectionsController
**Branch**: `refactor/controller-validation-srp`

### Summary

Standardized error response schemas to `ProblemDetails` across all controllers, added early-return input validation on bulk endpoints, and extracted a user-scoped endpoint into its own controller for single-responsibility. 12 files changed (74 insertions, 38 deletions).

### Key Changes

#### 1. BaseApiController → ProblemDetails on all error responses

**Before**
```csharp
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public abstract class BaseApiController : ControllerBase
```

**After**
```csharp
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public abstract class BaseApiController : ControllerBase
```

**Impact**: Swagger now generates accurate `ProblemDetails` schemas for all error responses. Eliminates need for individual controllers to redeclare `400`.

#### 2. Bulk endpoint input validation

**Before**
```csharp
public async Task<ActionResult<BulkMoveResult>> BulkMoveGroup(
    [FromBody] BulkMoveGroupDto dto, CancellationToken ct = default)
{
    var count = await bulkService.BulkMoveGroupAsync(dto, GetUserId(), ct);
    return Ok(new BulkMoveResult(count));
}
```

**After**
```csharp
public async Task<ActionResult<BulkMoveResult>> BulkMoveGroup(
    [FromBody] BulkMoveGroupDto dto, CancellationToken ct = default)
{
    if (dto.AssetIds is not { Count: > 0 })
        return BadRequest(new ProblemDetails { Title = "AssetIds must not be empty.", Status = 400 });

    var userId = GetUserId();
    logger.LogInformation("Bulk move-group requested for {Count} assets by {UserId}",
        dto.AssetIds.Count, userId);
    var count = await bulkService.BulkMoveGroupAsync(dto, userId, ct);
    return Ok(new BulkMoveResult(count));
}
```

Applied to all 4 bulk endpoints: `BulkDelete`, `BulkMove`, `BulkMoveGroup`, `BulkTag`.

**Impact**: Prevents unnecessary service calls with empty payloads; returns clear `ProblemDetails` error.

#### 3. SharedCollectionsController extraction (SRP)

**Before**
```csharp
// Inside PermissionsController (collection-scoped: /api/v1/collections/{id}/permissions)
[HttpGet("/api/v1/shared-collections")]  // absolute route override — code smell
public async Task<ActionResult<List<Collection>>> GetSharedCollections(...)
```

**After**
```csharp
// New dedicated controller
[Route("api/v1/shared-collections")]
public sealed class SharedCollectionsController(IPermissionService permissionService) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<List<Collection>>> GetSharedCollections(...)
}
```

**Impact**: Eliminates absolute route override; each controller has a single responsibility (collection-scoped CRUD vs user-scoped queries).

#### 4. Admin-only tag migration

```csharp
[HttpPost("migrate")]
[Authorize(Roles = "Admin")]
[ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public async Task<ActionResult<MessageResult>> MigrateCommaSeparatedTags(...)
```

**Impact**: Prevents non-admin users from triggering expensive migration operations.

---

## RF-005 — Controller Hardening & Magic String Elimination

**Date**: 2026-03-05
**Scope**: All backend controllers, service registration, DTOs
**Branch**: `refactor/controllers-and-services`

### Summary

Cross-cutting refactor targeting API robustness, type safety, and adherence to SOLID principles. 22 files changed (438 insertions, 242 deletions).

### Key Changes

#### 1. Magic String → Compile-Time Constants

**Before**
```csharp
[Authorize(Policy = "RequireAssetWrite")]   // silent failure on typo
```

**After**
```csharp
// PolicyNames.cs — centralized constants
internal static class PolicyNames
{
    public const string RequireAssetRead  = nameof(RequireAssetRead);
    public const string RequireAssetWrite = nameof(RequireAssetWrite);
}

[Authorize(Policy = PolicyNames.RequireAssetWrite)] // compile-time safe
```

Same pattern applied to rate-limit policies via `RateLimitPolicies.cs`.

**Impact**: A typo in any policy name is now a compile error, not a silent 403/500.

#### 2. HealthController → IHealthCheckService (SRP + DIP)

**Before**
```csharp
public class HealthController(AppDbContext context, IWebHostEnvironment env) : BaseApiController
{
    // 40 lines of inline database probing, storage checks, anonymous object construction
}
```

**After**
```csharp
public sealed class HealthController(IHealthCheckService healthCheckService) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetHealth(CancellationToken ct = default)
    {
        var result = await healthCheckService.CheckAsync(ct);
        return result.IsHealthy ? Ok(result) : StatusCode(503, result);
    }
}
```

**Impact**: Controller LOC reduced from ~40 → 6. Health logic independently testable; typed `HealthCheckResult` record for Swagger.

#### 3. Anonymous Objects → Typed DTOs

**Before**
```csharp
return Ok(new { role });
return Ok(new { message = "Tag migration completed successfully." });
return Ok(new { deleted = count });
```

**After**
```csharp
return Ok(new RoleResult(role));
return Ok(new MessageResult("Tag migration completed successfully."));
return Ok(new BulkDeleteResult(count));
```

New records: `RoleResult`, `MessageResult`, `BulkDeleteResult`, `BulkMoveResult`, `BulkTagResult`, `SearchRequestParams`.

**Impact**: Swagger generates accurate schemas; frontend consumers get predictable contracts.

#### 4. Route Constraints & Explicit Binding

**Before**
```csharp
[HttpGet("{id}")]
public async Task<ActionResult<Tag>> GetTag(int id, CancellationToken ct)
```

**After**
```csharp
[HttpGet("{id:int}")]
public async Task<ActionResult<Tag>> GetTag([FromRoute] int id, CancellationToken ct = default)
```

Applied consistently to all 18+ endpoints accepting route parameters.

**Impact**: Invalid routes (e.g., `/tags/abc`) rejected at routing level instead of model binding. Explicit `[FromRoute]`/`[FromBody]` removes ambiguity.

#### 5. Sealed Controllers & Primary Constructors

All controllers marked `sealed` (no subclassing intended). `AssetsCommandController` and `AssetsQueryController` converted from field-backed injection to primary constructors.

**Impact**: Reduced boilerplate; signals design intent clearly.

#### 6. Structured Logging

Added `ILogger<T>` to `PermissionsController` and `TagsController` with structured log messages for mutation operations (Grant, Revoke, Create, Delete, Migrate).

**Impact**: Audit trail for permission and tag mutations.

---

## RF-001 — CQRS Extraction for Asset Module

**Date**: 2026-02-27
**Scope**: Asset CRUD operations

### Before
```
AssetsController  (14 endpoints, single controller)
└── IAssetService (14 methods, direct calls)
```

### After
```
AssetsCommandController (6 write endpoints)
├── UploadAssetsCommand         → Handler → IAssetService
├── UpdateAssetCommand          → Handler → IAssetService
├── DeleteAssetCommand          → Handler → IAssetService
├── DuplicateAssetCommand       → Handler → IAssetDuplicateStrategyFactory
└── UpdateAssetPositionCommand  → Handler → IAssetService

AssetsQueryController (3 read endpoints)
├── GetAssetsQuery          → Handler → IAssetService
├── GetAssetByIdQuery       → Handler → IAssetService
└── GetAssetsByFolderQuery  → Handler → IAssetService

AssetApplicationService (Facade)
└── ISender + IUserContextProvider + IOptions<AssetOptions>
```

**Impact**: Controller LOC reduced from ~200 → 114 + 58. Each operation independently testable.

---

## RF-002 — TPH Inheritance for Asset Types

**Date**: 2026-02-26
**Scope**: Asset type differentiation

### Before
```csharp
// Switch statements scattered across services
switch (asset.ContentType)
{
    case "image": /* ... */ break;
    case "link":  /* ... */ break;
    // ...
}
```

### After
```csharp
// Virtual dispatch via TPH subtypes
public class Asset
{
    public virtual bool HasPhysicalFile => true;
    public virtual bool CanHaveThumbnails => false;
}

public class ImageAsset : Asset
{
    public override bool CanHaveThumbnails => true;
}
```

**Impact**: Eliminated 5+ switch statements. New asset types follow OCP.

---

## RF-003 — Strategy Pattern for Asset Duplication

**Date**: 2026-02-27
**Scope**: Duplicate asset operation

### Before
```csharp
// Single method with if/else for target
public async Task<Asset> DuplicateAsync(int id, int? targetFolderId)
{
    if (targetFolderId.HasValue)
        // copy to folder
    else
        // copy in place
}
```

### After
```csharp
public interface IAssetDuplicateStrategy
{
    bool CanHandle(DuplicateAssetRequest request);
    Task<Asset> ExecuteAsync(DuplicateAssetRequest request, CancellationToken ct);
}

// InPlaceDuplicateStrategy — copies in same location
// TargetFolderDuplicateStrategy — copies to specific folder
// AssetDuplicateStrategyFactory — selects correct strategy
```

**Impact**: New duplication modes (e.g., cross-collection) can be added without modifying existing strategies.

---

## RF-004 — Frontend OOP API Layer

**Date**: 2026-02-26
**Scope**: Frontend HTTP client

### Before
```javascript
// Standalone functions per endpoint
export const getAssets = (collectionId) => axios.get(`/api/v1/Assets?collectionId=${collectionId}`);
export const deleteAsset = (id) => axios.delete(`/api/v1/Assets/${id}`);
// ~60 standalone functions
```

### After
```javascript
class BaseApiService {
    constructor(endpoint) { this.endpoint = endpoint; this.client = apiClient; }
    async _get(path, params) { ... }
    async _post(path, data) { ... }
}

class AssetsApi extends BaseApiService {
    constructor() { super('/Assets'); }
    getAll(collId, params) { return this._get('', { collectionId: collId, ...params }); }
}

export default new AssetsApi();  // Singleton
```

**Impact**: 80% code reduction in API layer. 7 service classes inherit from 1 base. Consistent error handling and token attachment.

---

> **Document End**
