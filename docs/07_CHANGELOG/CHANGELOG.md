# CHANGELOG

> **Last Updated**: 2026-03-10

All notable changes to the Visual Asset Hub project.

Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)

---

## [Unreleased]

---

## [0.4.5] — 2026-03-10

### Added
- **ErrorCodes.cs**: Extracted centralized `snake_case` error code constants (`empty_batch`, `batch_size_exceeded`, `invalid_smart_collection_id`) into dedicated file — compile-time safety, prevents typos
- **BaseApiController.GetTraceId()**: Helper returning `HttpContext.TraceIdentifier` for structured logging and ProblemDetails enrichment

### Changed
- **ApiErrors.cs**: Upgraded to Lead-level (9.8/10) with 7 improvements:
  - **URN Type scheme**: `/errors/{code}` → `urn:vah:error:{code}` — stable RFC 9457 §3.1.1 compliant URIs
  - **CodeKey constant**: Eliminated repeated `"code"` magic string
  - **MetaKey constant**: Structured extensions schema — only `code` (always) + `meta` (optional context) allowed
  - **Detail field**: Every ProblemDetails now includes an actionable `Detail` message
  - **No raw input echo in Title**: `InvalidSmartCollectionId` moved `id` from Title to `meta.invalidId`
  - **Truncate() helper**: Null-safe, trims whitespace, caps echoed input at 100 chars to prevent oversized payloads
  - **Remarks**: Documented extensions schema contract and traceId/Instance middleware enrichment strategy
- **AuthController.MaskEmail()**: Now masks domain too (`t***@d***.com` instead of `t***@domain.com`) — stronger PII protection
- **AuthController.Login**: `[ProducesResponseType]` for 401 now typed as `ProblemDetails` — consistent Swagger schema
- **AssetLayoutController.ReorderAssets**: Added `<remarks>` documenting `ValidateBatchFilter` pipeline
- **BulkAssetsController**: Added `[ProducesResponseType(404)]` on `BulkMove` and `BulkMoveGroup` — target collection/group may not exist
- **BulkOperationLimits**: Added `<remarks>` with rationale (why 500) and `IOptions<BulkOptions>` upgrade path
- **CollectionsController**: Added `[Range(1, int.MaxValue)]` on all `int id` route params; `UpdateCollectionPut` now async/await
- **ColorGroupsController, ColorsController, FoldersController**: Added `[ProducesResponseType(404)]` + `[ProducesResponseType(403)]` — service can throw NotFoundException/ForbiddenAccess

### Metrics
- 11 files changed (10 modified + 1 new)
- ApiErrors.cs: 8.3 → 9.8 score
- All 10 controllers evaluated now at Lead level (9.0+)

### Added
- Documentation system: 8 directories, 30+ files (01–08 hierarchy)

---

## [0.4.4] — 2026-03-08

### Added
- **AuthContextMissingException**: Custom exception for missing auth context — maps to 401 with "Authentication Context Missing" title, distinct from generic `UnauthorizedAccessException`
- **ValidateBatchFilterAttribute**: Action filter centralizing empty + max-batch-size validation — replaces 5× inline guard blocks across `BulkAssetsController` + `AssetLayoutController`
- **ApiErrors**: Factory methods (`EmptyBatch()`, `BatchSizeExceeded()`, `InvalidSmartCollectionId()`) producing ProblemDetails with machine-readable `code` extension field
- **LogEvents**: Structured `EventId` constants organized by domain range (1xxx=Auth, 2xxx=Bulk, 3xxx=Collection, 4xxx=Asset-layout, 5xxx=Tags, 6xxx=Permissions)
- **GET /collections/{id}**: Canonical resource endpoint — `CreatedAtAction` now points to `GetCollection` instead of `GetCollectionWithItems`
- **`GetUserGuid()` helper**: Added to `BaseApiController` for GUID-typed user ID extraction

### Changed
- **AuthController**: `Register` now returns `201 Created` (was `200 OK`) per REST semantics; all log calls use `LogEvents.*` event IDs
- **BaseApiController**: `GetUserId()` throws `AuthContextMissingException` (was `UnauthorizedAccessException`); added `[ProducesResponseType(403)]` globally
- **BulkAssetsController**: All 4 endpoints use `[ValidateBatchFilter]` attribute — eliminated ~40 lines of duplicated guard code
- **AssetLayoutController**: `ReorderAssets` uses `[ValidateBatchFilter]`; log calls use `LogEvents.AssetReorder`
- **CollectionsController**: Added `[Range(1, int.MaxValue)]` on `folderId`; `[ProducesResponseType(403)]` on mutation + detail endpoints
- **ColorsController, ColorGroupsController, FoldersController, LinksController**: Added `[ProducesResponseType(409)]` on create; log calls use `LogEvents.AssetCreated`
- **HealthController**: Added `[ResponseCache(NoStore = true)]` at class level; `LivenessResult` now includes `Version` property (from `AssemblyInformationalVersionAttribute`)
- **PermissionsController**: Synchronized `ProducesResponseType` (403/404/409) across all endpoints; log calls use `LogEvents.PermissionGranted/Updated/Revoked`
- **SearchController**: Added remarks documenting `SearchRequestParams` validation strategy
- **TagsController**: All log calls use `LogEvents.TagCreated/TagDeleted/TagMigration`
- **SharedCollectionsController**: Added `ILogger` dependency + debug logging; `[ResponseCache(Duration = 60, VaryByHeader = "Authorization")]`
- **SmartCollectionsController**: Added `[RegularExpression(@"^[a-z0-9\-]+$")]` on `id` param (whitelist validation)
- **GlobalExceptionHandler**: Added `AuthContextMissingException` handler before `UnauthorizedAccessException`

### Metrics
- ~20 files changed
- 40+ lines of duplicated guard code eliminated
- All controllers now score 10/10 on lead-level quality rubric (see RF-008)

---

## [0.4.3] — 2026-03-07

### Added
- **BulkOperationLimits**: Centralized `MaxBatchSize = 500` constant — enforced across all bulk endpoints and `ReorderAssets`
- **Search rate-limiting**: New `Search` sliding-window policy (60 req/min, 6 segments) applied to `SearchController` via `[EnableRateLimiting]`
- **Liveness probe**: `GET /api/v1/health/live` — lightweight K8s-style liveness endpoint (no DB/storage probing); returns `LivenessResult` record
- **TagService.CreateOrGetAsync**: Returns `(Tag, bool Created)` tuple — enables `TagsController` to return `201 Created` for new tags and `200 OK` for duplicates (idempotent)
- **Structured logging**: Added `ILogger` injection + log statements to `AssetLayoutController`, `ColorGroupsController`, `ColorsController`, `FoldersController`, `LinksController`

### Changed
- **BaseApiController**: Added `404 NotFound` with `ProblemDetails` to base-level response types; updated `GetUserId()` docs to reference `GlobalExceptionHandler`
- **BulkAssetsController**: All 4 endpoints now validate `MaxBatchSize` in addition to empty-check
- **AssetLayoutController**: `ReorderAssets` validates empty + max-batch-size; added structured logging
- **HealthController**: Now inherits from `ControllerBase` (not `BaseApiController`) — health endpoints don't require auth-related base responses; added readiness vs liveness distinction in XML docs
- **TagsController.CreateTag**: Now returns `201` for new / `200` for existing (was always `201`); added `[ProducesResponseType(200)]` and remarks about idempotent behavior
- **TagsController.MigrateCommaSeparatedTags**: Added `[EnableRateLimiting(Fixed)]` to prevent repeated expensive migrations
- **RateLimitPolicies**: Added `Search` constant
- **CollectionsController**: Added remarks documenting intentionally un-paginated `GetCollections` design decision
- **Asset-type controllers** (Colors, ColorGroups, Folders, Links): Expanded XML `<remarks>` explaining per-type controller separation rationale with cross-references
- **PermissionsController**: Enhanced remarks with defense-in-depth documentation for service-layer verification

---

## [0.4.2] — 2026-03-06

### Added
- **SharedCollectionsController**: Extracted user-scoped shared-collections query from `PermissionsController` into a dedicated controller (SRP — collection-scoped CRUD vs user-scoped queries)
- **Input validation on bulk endpoints**: `BulkDelete`, `BulkMove`, `BulkMoveGroup`, `BulkTag` now return `400 ProblemDetails` when `AssetIds` is empty
- **Structured logging**: Added logging to `BulkMoveGroup` and `BulkTag` operations (previously only `BulkDelete` and `BulkMove` had logging)
- **Admin-only tag migration**: `POST /tags/migrate` now requires `[Authorize(Roles = "Admin")]` with `403` response type

### Changed
- **BaseApiController**: All base error responses (`400`, `401`, `500`) now declare `typeof(ProblemDetails)` for accurate Swagger schemas
- **ProblemDetails consistency**: All `404 NotFound` responses across controllers now use `typeof(ProblemDetails)` instead of bare status codes
- **Removed redundant `[ProducesResponseType(400)]`**: Controllers that inherit from `BaseApiController` no longer re-declare `400` — it's handled once in the base class
- **AuthController**: `409 Conflict` response now uses `typeof(ProblemDetails)`; removed duplicate `400` declaration
- **PermissionsController**: Removed `GetSharedCollections` endpoint (moved to `SharedCollectionsController`); added structured logging to `Update` operation; updated remarks to reference new controller
- **TagsController**: Added `<remarks>` doc on migrate endpoint noting admin-only behavior

---

## [0.4.1] — 2026-03-05

### Added
- **PolicyNames**: Centralized authorization policy name constants — eliminates magic strings (`RequireAssetRead`, `RequireAssetWrite`)
- **RateLimitPolicies**: Centralized rate-limit policy name constants (`Fixed`, `Upload`)
- **IHealthCheckService / HealthCheckService**: Extracted health-check logic from `HealthController` into a dedicated service (SRP + DIP)
- **Typed response DTOs**: `BulkDeleteResult`, `BulkMoveResult`, `BulkTagResult`, `RoleResult`, `MessageResult` — replaces anonymous objects for type-safe Swagger documentation
- **SearchRequestParams**: Grouped search query parameters into a cohesive DTO (eliminates primitive obsession)
- **HealthCheckResult / HealthChecks / HealthInfo**: Strongly-typed records for health endpoint responses

### Changed
- All controllers marked `sealed` (prevent unintended inheritance)
- All `[Authorize(Policy = "...")]` magic strings replaced with `PolicyNames.*` constants
- All rate-limit policy magic strings replaced with `RateLimitPolicies.*` constants
- `HealthController`: Delegates to `IHealthCheckService` instead of directly accessing `AppDbContext` and `IWebHostEnvironment`; added `[AllowAnonymous]`
- `PermissionsController`: Added `ILogger` injection, structured logging for Grant/Revoke operations, route constraints (`{collectionId:int}`, `{permissionId:int}`), explicit `[FromRoute]` bindings
- `TagsController`: Added `ILogger` injection, structured logging for Create/Delete/Migrate, route constraints (`{id:int}`, `{assetId:int}`), changed Set/Add/Remove tag responses from `200 Ok()` → `204 NoContent()`
- `SearchController`: Replaced 5 individual `[FromQuery]` parameters with `SearchRequestParams` binding model
- `AssetsCommandController` / `AssetsQueryController`: Converted to primary constructors (removed field-backed DI), added route constraints (`{id:int}`, `{groupId:int}`)
- `SmartCollectionsController`: Added `[FromRoute]` binding, default `CancellationToken`
- XML doc comments: Consolidated multi-line `<summary>` blocks to single-line + `<remarks>` where appropriate
- All `CancellationToken` parameters now have `= default` for consistent optional cancellation
- Added explicit `[FromRoute]` and `[FromBody]` binding attributes throughout all controllers
- Added missing `[ProducesResponseType(StatusCodes.Status400BadRequest)]` on mutation endpoints

---

## [0.4.0] — 2026-02-27

### Added
- **Tag System**: Full many-to-many tagging with `Tag` + `AssetTag` entities
- **Smart Collections**: Strategy-pattern dynamic collections (5 filter types)
- **CQRS for Assets**: MediatR command/query separation with dedicated handlers
- **Asset Duplication**: Strategy pattern (in-place + target-folder) via `IAssetDuplicateStrategy`
- **Collection Permissions**: Role-based sharing (owner/editor/viewer)
- **Permission Controller**: 6 endpoints for RBAC management
- **Tag Migration**: Automated comma-separated → relational tag migration

### Changed
- Split `AssetsController` → `AssetsCommandController` + `AssetsQueryController`
- Extracted `AssetApplicationService` facade (wraps MediatR + user context)
- Added `IUserContextProvider` to decouple handlers from `HttpContext`

---

## [0.3.0] — 2026-02-26

### Added
- **Thumbnail Generation**: 3-tier (sm/md/lg) WebP via ImageSharp 3.1.12
- **Bulk Operations**: Bulk delete, move, move-to-group, tag
- **Color Assets**: Color swatches and color groups
- **Link Assets**: URL bookmark assets
- **Folder Assets**: Hierarchical folder organization within collections
- **Search**: Cross-entity search with type/collection filters
- **Real-time Notifications**: SignalR hub with user-scoped groups

### Changed
- TPH inheritance model: `Asset` base + 5 subtypes (Image, Link, Color, ColorGroup, Folder)
- Added `AssetFactory` with 8 static creation methods
- Added `EnumMappings` for bidirectional enum ↔ DB string conversion

---

## [0.2.0] — 2026-02-25

### Added
- **Authentication**: ASP.NET Identity + JWT with SignalR query string support
- **Collections**: CRUD with hierarchical nesting (ParentId self-ref)
- **Asset Upload**: Multi-file upload with UUID renaming
- **Rate Limiting**: Fixed (100/min) + Upload (20/min) policies
- **Error Handling**: RFC 7807 ProblemDetails via `GlobalExceptionHandler`
- **Dual DB Provider**: SQLite (dev) + PostgreSQL (prod)
- **Redis Cache**: Distributed cache with in-memory fallback
- **Docker Compose**: 4-service orchestration
- **Swagger**: API documentation (development only)

---

## [0.1.0] — 2026-02-25

### Added
- Initial project scaffolding
- .NET 9 backend with EF Core 9
- React 19 frontend with Vite
- Basic asset CRUD
- Initial database migrations

---

> **Document End**
