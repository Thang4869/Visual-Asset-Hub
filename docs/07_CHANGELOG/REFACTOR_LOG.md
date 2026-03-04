# REFACTOR LOG

> **Last Updated**: 2026-03-05

Tracks completed refactoring efforts with before/after comparisons.

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
