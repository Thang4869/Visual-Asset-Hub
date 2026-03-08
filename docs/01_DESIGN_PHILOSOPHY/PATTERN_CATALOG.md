# PATTERN CATALOG — Design Patterns in VAH

> **Last Updated**: 2026-03-08  
> **Total Patterns Identified**: 20

---

## Creational Patterns

### Factory Method — `AssetFactory`
- **File**: [Models/AssetFactory.cs](../../VAH.Backend/Models/AssetFactory.cs)
- **Problem**: Creating correct TPH subtype (`ImageAsset`, `LinkAsset`, etc.) with consistent defaults
- **Solution**: Static factory methods (`CreateImage`, `CreateFolder`, `CreateColor`, `CreateLink`, `CreateColorGroup`, `CreateFile`, `Duplicate`, `FromDto`)
- **OCP**: Adding new asset type = add new factory method + TPH subclass

### Abstract Factory — `IAssetDuplicateStrategyFactory`
- **File**: [Features/Assets/Application/Duplicate/](../../VAH.Backend/Features/Assets/Application/Duplicate/)
- **Problem**: Duplicate behavior varies by target (in-place vs target folder)
- **Solution**: Factory creates correct strategy based on `targetFolderId`

### Factory Method — `ApiErrors`
- **File**: [Controllers/ApiErrors.cs](../../VAH.Backend/Controllers/ApiErrors.cs)
- **Problem**: ProblemDetails construction scattered across controllers with inconsistent format
- **Solution**: Static factory methods (`EmptyBatch()`, `BatchSizeExceeded()`, `InvalidSmartCollectionId()`) producing ProblemDetails with machine-readable `code` extension field
- **OCP**: Adding new error type = add new factory method

---

## Structural Patterns

### Facade — `ServiceCollectionExtensions`
- **File**: [Extensions/ServiceCollectionExtensions.cs](../../VAH.Backend/Extensions/ServiceCollectionExtensions.cs)
- **Problem**: `Program.cs` would be 200+ LOC of DI registrations
- **Solution**: 6 extension methods grouping registrations by concern (`AddCorsPolicy`, `AddDatabase`, `AddIdentityAndAuth`, `AddCachingServices`, `AddRateLimitingPolicies`, `AddApplicationServices`)

### Facade — `AssetApplicationService`
- **File**: [Features/Assets/Application/AssetApplicationService.cs](../../VAH.Backend/Features/Assets/Application/AssetApplicationService.cs)
- **Problem**: Controllers shouldn't know about MediatR, user context extraction, default collection IDs
- **Solution**: Thin facade wrapping `ISender` + `IUserContextProvider` + `AssetOptions`

### Adapter — `FileMapperService`
- **File**: [Features/Assets/Infrastructure/Files/](../../VAH.Backend/Features/Assets/Infrastructure/Files/)
- **Problem**: `IFormFile` is ASP.NET-specific; application layer shouldn't depend on it
- **Solution**: Maps `IFormFile[]` → `IReadOnlyCollection<UploadedFileDto>`

---

## Behavioral Patterns

### Strategy — `ISmartCollectionFilter`
- **File**: [Services/SmartCollectionFilters.cs](../../VAH.Backend/Services/SmartCollectionFilters.cs)
- **Problem**: Smart collections need different filter logic (recent, by type, untagged, etc.)
- **Solution**: `ISmartCollectionFilter` interface with 5 concrete strategies: `RecentDaysFilter`, `ContentTypeFilter`, `UntaggedFilter`, `WithThumbnailsFilter`, `TagFilter`
- **OCP**: New filter = new class + DI registration. Zero modifications to `SmartCollectionService`

### Strategy — `IAssetDuplicateStrategy`
- **File**: [Features/Assets/Application/Duplicate/](../../VAH.Backend/Features/Assets/Application/Duplicate/)
- **Problem**: Duplicate in-place vs duplicate to target folder have different logic
- **Solution**: `InPlaceDuplicateStrategy`, `TargetFolderDuplicateStrategy` selected by factory

### Template Method — `Asset` Virtual Properties
- **File**: [Models/Asset.cs](../../VAH.Backend/Models/Asset.cs)
- **Problem**: Different asset types need different behavior for cleanup, thumbnails, file presence
- **Solution**: `virtual bool HasPhysicalFile`, `CanHaveThumbnails`, `RequiresFileCleanup` — subtypes override

### Action Filter — `ValidateBatchFilterAttribute`
- **File**: [Controllers/Filters/ValidateBatchFilterAttribute.cs](../../VAH.Backend/Controllers/Filters/ValidateBatchFilterAttribute.cs)
- **Problem**: 5 endpoints had identical 8-line batch validation guard blocks (empty check + max-size check)
- **Solution**: `ActionFilterAttribute` reads `AssetIds` from request body via `IAssetIdsRequest` interface, delegates to `ApiErrors` factory for standardized ProblemDetails
- **DRY**: Eliminated ~40 lines of duplicated code across `BulkAssetsController` + `AssetLayoutController`

### Mediator — MediatR CQRS Pipeline
- **File**: [CQRS/Assets/](../../VAH.Backend/CQRS/Assets/)
- **Problem**: Decouple command sender from handler; enable pipeline behaviors
- **Solution**: `IRequest<T>` records + `IRequestHandler<TReq, TRes>` handlers. Controllers never know handler classes

### Observer — SignalR `AssetHub`
- **File**: [Hubs/AssetHub.cs](../../VAH.Backend/Hubs/AssetHub.cs) + [Services/NotificationService.cs](../../VAH.Backend/Services/NotificationService.cs)
- **Problem**: Multiple browser tabs/clients need real-time updates when data changes
- **Solution**: `AssetHub` manages user groups. `NotificationService.NotifyAssetChanged()` pushes events. Clients subscribe via `useSignalR` hook

### Command — CQRS Records
- **File**: [CQRS/Assets/Commands/AssetCommands.cs](../../VAH.Backend/CQRS/Assets/Commands/AssetCommands.cs)
- **Problem**: Encapsulate write operations as immutable data objects
- **Solution**: `sealed record CreateAssetCommand(...)`, `UploadFilesCommand(...)`, `DeleteAssetCommand(...)`, etc.

---

## Architectural Patterns

### CQRS (Command Query Responsibility Segregation)
- **Files**: `CQRS/Assets/Commands/`, `CQRS/Assets/Queries/`, `CQRS/Assets/Handlers/`
- **Problem**: Read and write operations have different optimization needs
- **Solution**: Separate `GetAssetsQuery` (read) from `CreateAssetCommand` (write). Each has dedicated handler

### Modular Monolith (Vertical Slices)
- **Files**: `Features/Assets/` (Commands/, Queries/, Application/, Infrastructure/, Common/, Contracts/)
- **Problem**: Traditional layered architecture scatters feature code across folders
- **Solution**: `Features/Assets/` contains everything for Asset feature. Older modules still in `Services/`, `Controllers/` (migration in progress)

---

## Concurrency & Infrastructure Patterns

### Singleton — `TokenManager` (Frontend)
- **File**: [src/api/TokenManager.js](../../VAH.Frontend/src/api/TokenManager.js)
- **Problem**: JWT token must be globally accessible, single source of truth
- **Solution**: Module-level singleton instance with private `#storageKey` field

### Singleton — `DatabaseProviderInfo`
- **File**: [Program.cs](../../VAH.Backend/Program.cs)
- **Problem**: Runtime DB provider detection needed across services
- **Solution**: `record DatabaseProviderInfo(string ProviderName)` registered as singleton

### Rate Limiter — Fixed Window
- **File**: [Extensions/ServiceCollectionExtensions.cs](../../VAH.Backend/Extensions/ServiceCollectionExtensions.cs)
- **Problem**: Protect API from abuse
- **Solution**: `Fixed` (100 req/min) and `Upload` (20 req/min) rate limiting policies

---

## Frontend Patterns

### Module Pattern — API Barrel Export
- **File**: [src/api/index.js](../../VAH.Frontend/src/api/index.js)
- **Problem**: Centralize API service access
- **Solution**: Single barrel file exports all API singletons (`assetApi`, `collectionApi`, `tagApi`, etc.)

### Inheritance — `BaseApiService` → Subclasses
- **File**: [src/api/BaseApiService.js](../../VAH.Frontend/src/api/BaseApiService.js)
- **Problem**: DRY HTTP methods across 7 API services
- **Solution**: `BaseApiService` provides `_get()`, `_post()`, `_put()`, `_patch()`, `_delete()`. Subclasses (`AssetsApi`, `CollectionsApi`, ...) extend and add domain methods

### Context + Hook Pattern — State Management
- **Files**: [src/context/](../../VAH.Frontend/src/context/), [src/hooks/](../../VAH.Frontend/src/hooks/)
- **Problem**: Global state without prop-drilling, separation of state logic from UI
- **Solution**: `AppContext` provides global state. 11 custom hooks encapsulate domain logic (`useAssets`, `useCollections`, `useTags`, etc.)

---

## Pattern Decision Guide

```
Need to create objects of varying types?
  └─→ Factory (AssetFactory) or Abstract Factory (DuplicateStrategyFactory)

Need interchangeable algorithms?
  └─→ Strategy (ISmartCollectionFilter, IAssetDuplicateStrategy)

Need to decouple sender from receiver?
  └─→ Mediator (MediatR) or Observer (SignalR)

Need shared base behavior with type-specific overrides?
  └─→ Template Method (Asset virtual properties)

Need to simplify a complex subsystem?
  └─→ Facade (ServiceCollectionExtensions, AssetApplicationService)

Need to adapt incompatible interfaces?
  └─→ Adapter (FileMapperService: IFormFile → UploadedFileDto)
```

---

> **Document End**  
> Related: [ARCHITECTURE_CONVENTIONS.md](ARCHITECTURE_CONVENTIONS.md) · [DESIGN_PRINCIPLES.md](DESIGN_PRINCIPLES.md)
