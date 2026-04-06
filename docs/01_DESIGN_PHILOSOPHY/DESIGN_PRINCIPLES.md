# Nguyên Lý Thiết Kế (Design Principles)

> **Mục đích**: Định nghĩa các nguyên lý SOLID, Clean Architecture và DDD cho VAH  
> **Last Updated**: 2026-04-06

---

## §1 — Tại Sao Các Nguyên Lý Này Quan Trọng

VAH là **Modular Monolith** phục vụ ≤50 users, hướng tới SaaS multi-instance (Q1 2028).

**Mục tiêu:**
1. Duy trì codebase khi team mở rộng (1 → 5 devs)
2. Refactor an toàn (testability-first)
3. Chuẩn bị modularization không over-engineer

---

## §2 — SOLID Principles

### §2.1 Single Responsibility (SRP)

**Metrics:** LOC ≤ 300, methods ≤ 15, dependencies ≤ 5

| Trước | Sau |
|-------|-----|
| `AssetService` (400+ LOC) | `AssetService` + `BulkAssetService` + `AssetCleanupHelper` |
| `App.jsx` (477 LOC) | 17 components nhỏ |

### §2.2 Open/Closed (OCP)

| Extension Point | Cơ chế |
|----------------|--------|
| Smart Collection filters | `ISmartCollectionFilter` + DI auto-discovery |
| Asset types | TPH inheritance + `AssetFactory` |
| Duplicate strategies | `IAssetDuplicateStrategy` + Factory |

### §2.3 Liskov Substitution (LSP)

```
✅ asset.HasPhysicalFile    → ImageAsset=true, LinkAsset=false
✅ asset.ToDto()            → Hoạt động đồng nhất cho mọi subtype
```

### §2.4 Interface Segregation (ISP)

- `IAssetService` (14 methods) vs `IBulkAssetService` (4 methods)
- `IStorageService` (4 methods) vs `IThumbnailService` (1 method)

### §2.5 Dependency Inversion (DIP)

```
Controllers → IApplicationService → ISender → IService → DbContext
```

---

## §3 — Clean Architecture

### §3.1 Mô Hình Layer

```
┌─────────────────────────────────────────┐
│        PRESENTATION (Web API)           │
│  Controllers, Hubs, Middleware          │
├─────────────────────────────────────────┤
│        APPLICATION (Use Cases)          │
│  Services, CQRS Handlers                │
├─────────────────────────────────────────┤
│          DOMAIN (Business)              │
│  Entities, Value Objects, Factory       │
├─────────────────────────────────────────┤
│        INFRASTRUCTURE (I/O)             │
│  DbContext, Storage, Cache              │
└─────────────────────────────────────────┘
```

### §3.2 VAH Mapping

| Layer | Folder |
|-------|--------|
| Presentation | `Controllers/`, `Hubs/`, `Middleware/` |
| Application | `Services/`, `CQRS/`, `Features/*/Application/` |
| Domain | `Models/` |
| Infrastructure | `Data/`, `Migrations/` |

---

## §4 — DDD Tactical Patterns

### §4.1 Bounded Contexts

| Domain | Components |
|--------|------------|
| **Core** | Asset, Collection, SmartCollections |
| **Supporting** | Tag, Search |
| **Generic** | Identity, Permissions, Real-Time, Storage |

### §4.2 Aggregate Roots

| Aggregate | Root | Invariants |
|-----------|------|------------|
| Asset | `Asset` | Must have owner & collection |
| Collection | `Collection` | Name required, owner required |
| Tag | `Tag` | Name unique per user |

---

## §5 — Quyết Định Kiến Trúc

### §5.1 Monolith vs Microservices

| Factor | Monolith | Microservices |
|--------|----------|---------------|
| Team nhỏ | ✅ Simple | ❌ Overhead |
| Data consistency | ✅ Single DB | ❌ Distributed |

### §5.2 TPH vs TPT

**Chọn TPH:** Single table scan, 6 types với minimal unique columns.

### §5.3 CQRS + MediatR

**Lý do:** Separate read/write, pipeline behaviors cho logging/validation.

---

> **Document End**  
> Related: [ARCHITECTURE_CONVENTIONS.md](ARCHITECTURE_CONVENTIONS.md) · [PATTERN_CATALOG.md](PATTERN_CATALOG.md)
