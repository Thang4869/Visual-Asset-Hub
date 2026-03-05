# CHANGELOG

> **Last Updated**: 2026-03-06

All notable changes to the Visual Asset Hub project.

Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)

---

## [Unreleased]

### Added
- Documentation system: 8 directories, 30+ files (01–08 hierarchy)

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
