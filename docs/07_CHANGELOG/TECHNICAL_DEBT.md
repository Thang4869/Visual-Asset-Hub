# TECHNICAL DEBT REGISTER

> **Last Updated**: 2026-03-08
> **Source**: Extracted from ARCHITECTURE_REVIEW.md §7 + code analysis

---

## Severity Scale

| Level | Impact | Action Deadline |
|-------|--------|----------------|
| 🔴 Critical | Blocks scaling or causes data loss | Next sprint |
| 🟠 High | Significant maintenance burden | Within 2 sprints |
| 🟡 Medium | Code quality / consistency issue | Within quarter |
| 🟢 Low | Nice-to-have improvement | Backlog |

---

## Active Debt Items

### TD-001 — Zero Test Coverage 🔴

**Area**: Entire project
**Impact**: No regression safety net; refactoring is risky
**Remediation**:
1. Unit tests for domain models (Asset, Collection, Tag domain methods)
2. Unit tests for CQRS handlers (mock IAssetService)
3. Integration tests for API endpoints (WebApplicationFactory)
4. Frontend: Component tests with Vitest + Testing Library

### TD-002 — No Repository Layer 🟡

**Area**: Services directly use `AppDbContext`
**Impact**: EF Core coupling in service layer; harder to mock in tests
**Current Justification**: <10 entities, team of 1–3
**Re-evaluate**: At 15+ entities or when adding a second data source

### TD-003 — No Token Refresh 🟠

**Area**: `TokenManager.js` + `AuthService`
**Impact**: Users must re-login when JWT expires
**Remediation**: Implement refresh token rotation (secure HttpOnly cookie for refresh, short-lived access token)

### TD-004 — Legacy Exception Middleware 🟢

**Area**: `ExceptionHandlingMiddleware.cs` (83 lines)
**Impact**: Duplicates `GlobalExceptionHandler` functionality
**Remediation**: Remove middleware, rely solely on `IExceptionHandler` (RFC 7807)

### TD-005 — Legacy Comma-Separated Tags Field 🟡

**Area**: `Asset.Tags` (string field)
**Impact**: Redundant with `AssetTag` junction table; confusion in search queries
**Remediation**: Run `MigrateCommaSeparatedTagsAsync()` for all users, then deprecate field

### TD-006 — No Contract Tests 🟠

**Area**: Frontend ↔ Backend API boundary
**Impact**: Breaking API changes not caught until runtime
**Remediation**: Pact or OpenAPI-based contract testing in CI

### TD-007 — Auto-Migration in All Environments 🟡

**Area**: `Program.cs` → `context.Database.Migrate()`
**Impact**: Production migrations should be explicit, not auto-applied on startup
**Remediation**: Gate auto-migration behind `ASPNETCORE_ENVIRONMENT == Development`

### TD-008 — Single-Instance Storage 🟠

**Area**: `LocalStorageService` → filesystem
**Impact**: Cannot scale horizontally; files lost if container destroyed without volume
**Remediation**: Implement S3-compatible `IStorageService` (MinIO for dev, AWS S3 for prod)

### TD-009 — No CI/CD Pipeline 🔴

**Area**: Build & deployment
**Impact**: No automated testing, no deployment automation
**Remediation**: GitHub Actions / GitLab CI with build → test → deploy stages

### TD-010 — No Pagination on Collection List 🟢

**Area**: `ICollectionService.GetAllAsync` returns `List<Collection>`
**Impact**: Performance degrades with many collections
**Remediation**: Return `PagedResult<Collection>` with `PaginationParams`

### TD-011 — Authorization Policies Not Granular 🟡

**Area**: `RequireAssetRead` / `RequireAssetWrite` both resolve to `RequireAuthenticatedUser()`
**Impact**: No role-based differentiation
**Remediation**: Implement proper policy-based authorization using `CollectionPermission` roles

### TD-012 — No Input Validation Pipeline 🟡

**Area**: Command/query DTOs
**Impact**: Validation scattered across services instead of centralized
**Partial Mitigation (v0.4.4)**: `ValidateBatchFilterAttribute` centralizes batch validation for 5 endpoints; `ApiErrors` factory standardizes ProblemDetails with machine-readable `code` extension; `[Range]` and `[RegularExpression]` annotations added at controller level
**Remediation**: MediatR `IPipelineBehavior<,>` with FluentValidation for full coverage

---

## Resolved Debt

| ID | Description | Resolved In | Resolution |
|----|------------|-------------|-----------|
| — | Fat AssetsController (14 methods) | v0.4.0 | Split into Command + Query controllers |
| — | No CQRS pattern | v0.4.0 | MediatR + handlers for Asset module |
| — | No tag system | v0.4.0 | Full M:N tagging with Tag + AssetTag || — | Missing HealthController DI abstraction | v0.4.1 | Extracted `IHealthCheckService` (SRP + DIP) |
| — | Duplicated bulk validation guards (5×) | v0.4.4 | `ValidateBatchFilterAttribute` action filter (DRY) |
| — | No structured log event IDs | v0.4.4 | `LogEvents` constants by domain range |
---

> **Document End**
