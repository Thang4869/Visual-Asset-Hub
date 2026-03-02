# CHANGELOG

> **Last Updated**: 2026-03-02

All notable changes to the Visual Asset Hub project.

Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)

---

## [Unreleased]

### Added
- Documentation system: 8 directories, 30+ files (01–08 hierarchy)

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
