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
