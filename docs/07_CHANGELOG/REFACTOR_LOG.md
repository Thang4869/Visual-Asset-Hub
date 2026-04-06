# Nhật ký Refactoring (Refactor Log)

> **Mục đích**: Ghi chép các refactoring đã hoàn thành với before/after.  
> **Last Updated**: 2026-04-06

---

## Tổng quan

| ID | Ngày | Tên | Phạm vi |
|----|------|-----|---------|
| RF-014 | 2026-03-25 | ValueObjects Hardening | AssetPosition, FileName, ColorCode |
| RF-013 | 2026-03-20 | Asset Validator & DI | Validator, Factory, Mapper lifetimes |
| RF-012 | 2026-03-17 | ApplicationUser Encapsulation | Auth entity, audit timestamps |
| RF-011 | 2026-03-14 | UploadedFileDto | IUploadedFile, metadata DTO, validation |
| RF-010 | 2026-03-13 | Asset Domain Model Hardening | Abstract class, private setters |
| RF-009 | 2026-03-12 | CQRS Immutability | IReadOnlyList, AssetOptions |
| RF-008 | 2026-03-12 | Three-Tier Bootstrap | Program.cs, 12 new infra files |
| RF-007 | 2026-03-10 | ApiErrors & Controllers | ProblemDetails RFC 9457 |
| RF-006 | 2026-03-08 | Lead-Level Controllers | Batch filters, error codes |
| RF-005 | 2026-03-07 | Batch Guards & Rate Limiting | BulkOperationLimits, search policy |
| RF-004 | 2026-03-06 | ProblemDetails Consistency | SRP extraction, validation |
| RF-003 | 2026-03-05 | Controller Hardening | Magic strings, typed DTOs |
| RF-002 | 2026-02-27 | CQRS Extraction | Asset module command/query split |
| RF-001 | 2026-02-26 | TPH Inheritance | Asset subtypes, Strategy pattern |

---

## RF-014 — ValueObjects Hardening (2026-03-25)

**Phạm vi**: `Models/ValueObjects/AssetPosition.cs`, `FileName.cs`, `ColorCode.cs`

**Thay đổi**:
- `AssetPosition`: Thêm `Zero` constant, `Deconstruct()`, exception với `nameof()`
- `FileName`: Null-check, `TryParse()` helper, validation exception
- `ColorCode`: Whitespace guard, normalization validation

**Breaking**: Callers dựa vào `FileName(null)` silent acceptance phải migrate sang `TryParse`.

---

## RF-013 — Asset Validator & DI Refactor (2026-03-20)

**Phạm vi**: DI registrations, `AssetValidatorImpl`, `AssetMapper` lifetimes

**Thay đổi**:
- `AssetValidator` → `internal` (public API via `DefaultAssetValidator.Instance`)
- `IAssetValidator` → `Singleton` concrete `AssetValidatorImpl`
- `IAssetMapper` → `Scoped` (depends on scoped `IAssetFactory`)

**Trade-offs**: Static `AssetFactory._impl` vẫn bypass DI container.

---

## RF-012 — ApplicationUser Encapsulation (2026-03-17)

**Phạm vi**: `ApplicationUser` entity, `AuthService`

**Thay đổi**:
- `DisplayName`, `CreatedAt` → private setters
- Domain method `SetDisplayName(string)` với validation + `UpdatedAt` audit
- Migration `AddApplicationUserUpdatedAt` thêm nullable `UpdatedAt` column

---

## RF-011 — UploadedFileDto Improvements (2026-03-14)

**Phạm vi**: `UploadedFileDto`, `IUploadedFile`, `UploadedFileMetadataDto`

**Thay đổi**:
- `IUploadedFile` interface cho testability
- `UploadedFileMetadataDto` cho serialization boundaries
- `OpenStreamAsync` overload, filename validation (max 260 chars)
- `IUploadedFileValidator` để verify stream length

---

## RF-010 — Asset Domain Model Hardening (2026-03-13)

**Phạm vi**: Asset aggregate root, TPH subtypes, AssetFactory, AssetMapper

**Thay đổi**:
- `Asset` → abstract class với protected constructors
- All properties → private setters
- Domain methods: `Rename()`, `Reorder()`, `AssignToGroup()`, `MoveToFolder()`, `SoftDelete()`
- `AssetValidator` với `[GeneratedRegex]` cho validation
- `AssetMapper` tách DTO mapping ra khỏi entity

**Tech debt resolved**: Public setters, ToDto() in entity, switch-based Duplicate

---

## RF-009 — CQRS Immutability & AssetOptions (2026-03-12)

**Phạm vi**: Commands, Queries, Handlers, Service interfaces, AssetOptions

**Thay đổi**:
- `List<>` → `IReadOnlyList<>` qua toàn bộ CQRS → Service chain
- `AssetOptions` thêm `[Range]` validation + `ValidateOnStart()`
- DI: `IAssetFactory` (Scoped), `IAssetMapper` (Singleton)

**Files changed**: 10 files (Commands, Queries, Handlers, Services, AssetOptions)

---

## RF-008 — Three-Tier Bootstrap (2026-03-12)

**Phạm vi**: Program.cs, 11 new infrastructure files

**Thay đổi**:
- Program.cs: 180 lines → 46 lines orchestrator
- Three-tier: `AddCoreHosting()` → `AddApplication()` → `AddWeb()`
- OpenTelemetry tracing + metrics
- HTTP resilience với Polly 8
- Security headers middleware
- Health probes + API versioning

**New files**: BootstrapExtensions, WebServerSetup, ObservabilitySetup, LoggingSetup, SecuritySetup, IStartupInitializer, DatabaseMigrationInitializer, StartupInitializerExtensions, SecurityHeadersMiddleware, RouteConstants, DatabaseProviderInfo

---

## RF-007 — ApiErrors & Controllers Lead-Level (2026-03-10)

**Phạm vi**: ApiErrors, ErrorCodes, 10 controllers

**Thay đổi**:
- URN Type scheme: `/errors/{code}` → `urn:vah:error:{code}` (RFC 9457)
- `ErrorCodes.cs`: Centralized snake_case error constants
- `Truncate()` helper: Input sanitization (max 100 chars)
- `MaskEmail()`: Domain cũng được mask (`t***@d***.com`)
- `[ProducesResponseType]` đầy đủ cho 401/403/404/409

**Quality**: 10 controllers upgraded từ Senior (8.2–8.7) → Lead (9.0–9.8)

---

## RF-006 — Lead-Level Controllers (2026-03-08)

**Phạm vi**: BulkAssetsController, AssetLayoutController, ValidateBatchFilterAttribute

**Thay đổi**:
- `ValidateBatchFilterAttribute`: Centralized empty + max-batch validation
- `AuthContextMissingException`: Custom exception cho missing auth context
- `LogEvents`: Structured EventId constants organized by domain (1xxx-6xxx)
- `GET /collections/{id}`: Canonical resource endpoint

**Tech debt resolved**: 40+ lines duplicated guard code eliminated

---

## RF-005 — Batch Guards & Rate Limiting (2026-03-07)

**Phạm vi**: BulkOperationLimits, RateLimitPolicies, SearchController, HealthController

**Thay đổi**:
- `BulkOperationLimits.MaxBatchSize = 500`
- `Search` rate-limit policy: 60 req/min sliding window
- `GET /api/v1/health/live`: K8s liveness probe
- `TagService.CreateOrGetAsync`: `(Tag, bool Created)` tuple cho idempotent behavior

---

## RF-004 — ProblemDetails Consistency (2026-03-06)

**Phạm vi**: BaseApiController, BulkAssetsController, SharedCollectionsController, TagsController

**Thay đổi**:
- `SharedCollectionsController`: Extracted từ PermissionsController (SRP)
- All `typeof(ProblemDetails)` declarations trên 400/401/404/409
- Admin-only tag migration: `[Authorize(Roles = "Admin")]`
- Structured logging cho bulk operations

---

## RF-003 — Controller Hardening (2026-03-05)

**Phạm vi**: 15 controllers, PolicyNames, RateLimitPolicies, typed response DTOs

**Thay đổi**:
- `PolicyNames`, `RateLimitPolicies`: Magic strings → constants
- Typed DTOs: `BulkDeleteResult`, `BulkMoveResult`, `BulkTagResult`, `RoleResult`, `MessageResult`
- `SearchRequestParams`: Grouped query parameters
- All controllers marked `sealed`
- Route constraints: `{id:int}`, `{collectionId:int}`

---

## RF-002 — CQRS Extraction (2026-02-27)

**Phạm vi**: Asset module, MediatR

**Thay đổi**:
- Split `AssetsController` → `AssetsCommandController` + `AssetsQueryController`
- `AssetApplicationService` facade (wraps ISender + IUserContextProvider)
- `IUserContextProvider`: Decouple handlers từ HttpContext
- 15+ Commands/Queries với dedicated handlers

---

## RF-001 — TPH Inheritance (2026-02-26)

**Phạm vi**: Asset entity, 5 subtypes

**Thay đổi**:
- `Asset` base class + 5 TPH subtypes: `FileAsset`, `ImageAsset`, `VideoAsset`, `LinkAsset`, `FolderAsset`
- `ISmartCollectionFilter`: Strategy pattern cho Smart Collections
- 5 filter strategies: Recent, Favorites, ByType, ByTag, ByDateRange

---

> **Document End**

#### 3. DRY Bulk Validation via Action Filter (8.5 → 10)

**Before** — 5 endpoints each had 8 lines of identical guard code:
```csharp
if (dto.AssetIds is not { Count: > 0 })
    return BadRequest(new ProblemDetails { Title = "AssetIds must not be empty.", Status = 400 });
if (dto.AssetIds.Count > BulkOperationLimits.MaxBatchSize)
    return BadRequest(new ProblemDetails { ... });
```

**After** — Single `[ValidateBatchFilter]` attribute:
```csharp
[HttpPost("bulk-delete")]
[ValidateBatchFilter]
public async Task<ActionResult<BulkDeleteResult>> BulkDelete(...)
```

The filter uses `ApiErrors.EmptyBatch()` / `ApiErrors.BatchSizeExceeded()` which include machine-readable `code` extensions:
```json
{ "title": "Batch size exceeds the maximum of 500.", "status": 400, "code": "batch_size_exceeded" }
```

Applied to: `BulkDelete`, `BulkMove`, `BulkMoveGroup`, `BulkTag`, `ReorderAssets`.

#### 4. CollectionsController — Canonical GET + 403 + folderId Constraint (8.9 → 10)

- Added `GET /collections/{id}` (canonical resource endpoint) so `CreatedAtAction` points to the right resource.
- `CreatedAtAction(nameof(GetCollection), ...)` instead of `nameof(GetCollectionWithItems)`.
- Added `[Range(1, int.MaxValue)]` constraint on `folderId` query param.
- Added `[ProducesResponseType(StatusCodes.Status403Forbidden)]` on all mutation + detail endpoints.

#### 5. Asset-Type Controllers — 409 + Event IDs (8.8 → 10)

Colors, ColorGroups, Folders, Links controllers:
- Added `[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]` for create endpoints.
- All log calls now use `LogEvents.AssetCreated` event ID.

#### 6. HealthController — Cache Headers + Build Info (9.1 → 10)

- Added `[ResponseCache(NoStore = true)]` at class level to prevent stale health data.
- `LivenessResult` now includes `Version` property (from `AssemblyInformationalVersionAttribute`).

```csharp
public sealed record LivenessResult(string Status, DateTime Timestamp, string Version);
```

#### 7. PermissionsController — Consistent Swagger + Event IDs (8.7 → 10)

- Synchronized `ProducesResponseType` across all endpoints: List/Grant now include 404, Grant includes 409, my-role includes 404.
- All mutations use `LogEvents.PermissionGranted/Updated/Revoked`.

#### 8. SearchController — Documented Validation Strategy (8.6 → 10)

- Added remarks documenting that `SearchRequestParams` already enforces `[Range]` via Data Annotations.
- Pagination bounds already present in DTO (`Page ≥ 1`, `PageSize 1–200`).

#### 9. TagsController — Event IDs (8.8 → 10)

- All log calls now use `LogEvents.TagCreated/TagDeleted/TagMigration`.

#### 10. SharedCollectionsController — Caching + Logging (8.7 → 10)

- Added `ILogger<SharedCollectionsController>` dependency.
- Added `[ResponseCache(Duration = 60, VaryByHeader = "Authorization")]` for slow-changing shared data.
- Debug-level logging for query operations.

#### 11. SmartCollectionsController — ID Validation + Pagination Bounds (8.7 → 10)

- Added `[RegularExpression(@"^[a-z0-9\-]+$")]` on `id` route parameter (whitelist validation).
- Pagination bounds already enforced via `PaginationParams` annotations (`Page ≥ 1`, `PageSize 1–100`).

#### 12. GlobalExceptionHandler — AuthContextMissingException

Added distinct handling for `AuthContextMissingException` (before `UnauthorizedAccessException`) with a more specific title: "Authentication Context Missing".

### Across-the-Board Improvements

| Improvement | Impact |
|---|---|
| `ValidateBatchFilterAttribute` | Eliminated 40 lines of duplicated guard code across 5 endpoints |
| `ApiErrors` factory | Machine-readable `code` field in every ProblemDetails response |
| `LogEvents` constants | Deterministic log filtering/alerting by EventId across all domains |
| `AuthContextMissingException` | Clean separation of "no identity" vs "forbidden" semantics |
| `ProducesResponseType(403)` on BaseApiController | All controllers inherit 403 Swagger documentation |
| `ProducesResponseType(409)` on create endpoints | Swagger accurately documents conflict scenarios |
| `[ResponseCache(NoStore = true)]` on health | Prevents load balancers from caching stale health data |
| `LivenessResult.Version` | Operational debugging without SSH/logs |

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
