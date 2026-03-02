# REFACTOR LOG

> **Last Updated**: 2026-03-02

Tracks completed refactoring efforts with before/after comparisons.

---

## RF-001 — CQRS Extraction for Asset Module

**Date**: 2026-02-27
**Scope**: Asset CRUD operations

### Before
```
AssetsController (14 endpoints, single controller)
└── IAssetService (14 methods, direct calls)
```

### After
```
AssetsCommandController (6 write endpoints)
├── UploadAssetsCommand → Handler → IAssetService
├── UpdateAssetCommand → Handler → IAssetService
├── DeleteAssetCommand → Handler → IAssetService
├── DuplicateAssetCommand → Handler → IAssetDuplicateStrategyFactory
└── UpdateAssetPositionCommand → Handler → IAssetService

AssetsQueryController (3 read endpoints)
├── GetAssetsQuery → Handler → IAssetService
├── GetAssetByIdQuery → Handler → IAssetService
└── GetAssetsByFolderQuery → Handler → IAssetService

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
