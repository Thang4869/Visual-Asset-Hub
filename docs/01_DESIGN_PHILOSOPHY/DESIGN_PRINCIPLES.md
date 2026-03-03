# DESIGN PRINCIPLES — SOLID, Clean Architecture & DDD Rationale

> **Document Type**: Prescriptive (MUST follow)  
> **Scope**: VAH Full Stack  
> **Last Updated**: 2026-03-02

---

## §1 — Why These Principles Matter for VAH

VAH is a **Modular Monolith** targeting ≤50 concurrent users today, with a North Star to become multi-instance SaaS by Q1 2028. The principles below are chosen to:

1. **Keep the codebase maintainable** as the team scales from 1 → 3 → 5 developers
2. **Enable safe refactoring** without regression (testability-first)
3. **Prepare for modularization** without premature abstraction

---

## §2 — SOLID Principles — VAH Policy

### 2.1 Single Responsibility (SRP)

**Policy**: Each class has ONE reason to change. Measured by:
- LOC ≤ 300 per class
- ≤ 15 public methods per class
- ≤ 5 constructor dependencies

**VAH Examples (Applied)**:

| Before (violated SRP) | After (correct) | Change Reason Separated |
|----------------------|-----------------|------------------------|
| `AssetService` (400+ LOC) | `AssetService` (CRUD) + `BulkAssetService` (batch) + `AssetCleanupHelper` (file cleanup) | CRUD ≠ Batch ≠ Cleanup |
| `App.jsx` (477 LOC) | `AppHeader` + `AppSidebar` + `AssetGrid` + `CollectionBrowser` + ... (17 components) | Layout ≠ Navigation ≠ Display |
| Monolithic `api.js` | `BaseApiService` + 7 subclasses (`AssetsApi`, `AuthApi`, ...) | Each API domain is separate |

### 2.2 Open/Closed (OCP)

**Policy**: Extend behavior by adding new classes, not modifying existing ones.

**VAH Examples (Applied)**:

| Extension Point | Mechanism | How to Extend |
|----------------|-----------|---------------|
| Smart Collection filters | `ISmartCollectionFilter` + `IEnumerable<>` injection | Add new class implementing interface → auto-discovered |
| Asset types | TPH inheritance (`Asset` → `ImageAsset`, etc.) | Add new subclass + `AssetFactory` method |
| Duplicate strategies | `IAssetDuplicateStrategy` + Factory | Add new strategy class |
| Exception → HTTP mapping | `GlobalExceptionHandler` switch expression | Add new case for new exception type |

### 2.3 Liskov Substitution (LSP)

**Policy**: Any `Asset` subtype must work correctly wherever `Asset` is expected.

**VAH Compliance**:
```
✅ asset.HasPhysicalFile    → ImageAsset=true, LinkAsset=false (caller never type-checks)
✅ asset.ToDto()            → Works identically for all subtypes
✅ asset.IsOwnedBy(userId)  → Inherited behavior, never overridden incorrectly
```

### 2.4 Interface Segregation (ISP)

**Policy**: No client should depend on methods it doesn't use.

**VAH Splits**:
- `IAssetService` (14 methods: CRUD) vs `IBulkAssetService` (4 methods: batch ops)
- `IStorageService` (4 methods: file I/O) vs `IThumbnailService` (1 method: image processing)
- `IAssetApplicationService` (8 methods: orchestration) vs `IAssetService` (14 methods: data)

### 2.5 Dependency Inversion (DIP)

**Policy**: High-level modules depend on abstractions, never on concretions.

**VAH Dependency Direction**:
```
Controllers →   IAssetApplicationService (interface)
                    ↓   impl
                AssetApplicationService     → ISender (MediatR)
                    ↓   handler
                CommandHandler              → IAssetService (interface)
                    ↓   impl
                AssetService                → AppDbContext, IStorageService, IThumbnailService
```

---

## §3 — Clean Architecture — VAH Adaptation

### 3.1 Layer Model

```
┌───────────────────────────────────────────┐
│          PRESENTATION (Web API)           │
│  Controllers, Hubs, Middleware            │
│  Knows: Application interfaces, DTOs      │
│  Never: Direct DB, Business logic         │
├───────────────────────────────────────────┤
│          APPLICATION (Use Cases)          │
│  Services, CQRS Handlers, AppServices     │
│  Knows: Domain, Infrastructure interfaces │
│  Never: HTTP, Controller, View concerns   │
├───────────────────────────────────────────┤
│          DOMAIN (Business Rules)          │
│  Entities, Value Objects, Enums, Factory  │
│  Knows: Only itself (pure C#)             │
│  Never: EF, HTTP, DI, frameworks          │
├───────────────────────────────────────────┤
│          INFRASTRUCTURE (I/O)             │
│  DbContext, Storage, Cache, External APIs │
│  Knows: Domain (to persist it)            │
│  Never: Application logic, Controllers    │
└───────────────────────────────────────────┘
```

### 3.2 Current VAH Mapping

| Layer | Namespace/Folder | Key Files |
|-------|-----------------|-----------|
| Presentation | `Controllers/`, `Features/*/Commands/`, `Features/*/Queries/`, `Hubs/`, `Middleware/` | 14 controllers, `AssetHub`, `GlobalExceptionHandler` |
| Application | `Services/`, `CQRS/*/Handlers/`, `Features/*/Application/` | `AssetService`, `CollectionService`, `AssetApplicationService`, MediatR handlers |
| Domain | `Models/` | `Asset`, `Collection`, `Tag`, `AssetFactory`, `Enums`, DTOs |
| Infrastructure | `Data/`, `Services/LocalStorageService`, `Migrations/` | `AppDbContext`, `LocalStorageService`, `ThumbnailService` |

### 3.3 Dependency Rule Violations to Fix

| Violation | Current State | Target |
|-----------|--------------|--------|
| Entity has `[Required]` DataAnnotation | `Asset.cs`, `Collection.cs` | Move to Fluent API in `AppDbContext` |
| Service directly uses `AppDbContext` | All services | Introduce `IRepository<T>` (Phase: Modularize) |
| DTO validation via DataAnnotations | `DTOs.cs` | Accept for now (simple & effective), FluentValidation later |

---

## §4 — Domain-Driven Design (DDD) — Tactical Patterns

### 4.1 Bounded Contexts

```
┌───────────────────────────────────────────────────────┐
│              CORE DOMAIN                              │
│                                                       │
│  Asset Management (Asset, AssetFactory, AssetTypes)   │
│  Collection Management (Collection)                   │
│  Smart Collections (ISmartCollectionFilter)           │
├───────────────────────────────────────────────────────┤
│              SUPPORTING DOMAIN                        │
│                                                       │
│  Organization (Tag, AssetTag)                         │
│  Search (ISearchService)                              │
├───────────────────────────────────────────────────────┤
│              GENERIC DOMAIN                           │
│                                                       │
│  Identity (ApplicationUser, AuthService)              │
│  Permissions (CollectionPermission, PermissionService)│
│  Real-Time (AssetHub, NotificationService)            │
│  Storage (IStorageService, IThumbnailService)         │
└───────────────────────────────────────────────────────┘
```

### 4.2 Aggregate Roots

| Aggregate | Root Entity | Children | Invariants |
|-----------|------------|----------|------------|
| **Asset** | `Asset` | `AssetTag` (M:N link) | Must have owner, must belong to collection |
| **Collection** | `Collection` | `Asset[]`, `Children[]`, `CollectionPermission[]` | Name required, owner required |
| **Tag** | `Tag` | `AssetTag[]` | Name unique per user (via NormalizedName) |

### 4.3 Domain Events (Future)

```
AssetCreated        → Trigger: thumbnail generation, SignalR notification
AssetDeleted        → Trigger: file cleanup, cache invalidation
CollectionShared    → Trigger: email notification, activity log
```

---

## §5 — Design Decision Rationale

### 5.1 Why Modular Monolith, Not Microservices?

| Factor | Monolith | Microservices |
|--------|----------|---------------|
| Team size (1-2 devs) | ✅ Simple deployment | ❌ Orchestration overhead |
| Data consistency | ✅ Single DB transaction | ❌ Distributed transactions |
| Development speed | ✅ Refactor easily | ❌ Service boundaries rigid |
| Scale trigger | 50+ concurrent, 3+ devs | — |

### 5.2 Why TPH, Not TPT?

| Factor | TPH (Table-Per-Hierarchy) | TPT (Table-Per-Type) |
|--------|--------------------------|---------------------|
| Query performance | ✅ Single table scan | ❌ JOIN per subtype |
| Schema simplicity | ✅ 1 table, discriminator | ❌ N tables |
| Null columns | ❌ Some nullable columns | ✅ No nulls |
| VAH decision | **✅ Chosen** — 6 types, minimal unique columns | — |

### 5.3 Why CQRS at Application Layer?

| Factor | Direct Service | CQRS + MediatR |
|--------|---------------|----------------|
| Read/write optimization | ❌ Same path | ✅ Separate handlers |
| Cross-cutting (logging, validation) | Manual per service | ✅ Pipeline behaviors |
| Controller coupling | Direct to service | ✅ Via `ISender` |
| Complexity | Simple | Moderate (justified by pipeline benefits) |

---

> **Document End**  
> Related: [ARCHITECTURE_CONVENTIONS.md](ARCHITECTURE_CONVENTIONS.md) · [PATTERN_CATALOG.md](PATTERN_CATALOG.md)
