# 📚 VAH — Mục lục Tài liệu

> **Visual Asset Hub** — Nền tảng Quản lý Tài nguyên Số  
> **Stack**: .NET 9 · React 19 · PostgreSQL 17 · Redis  
> **Last Updated**: 2026-04-06  
> **Status**: ✅ 35+ tập tin tài liệu đã được tạo và kiểm chứng

---

## §1 — Tổng quan

### 1.1 Thống kê

| Metric | Giá trị |
|--------|---------|
| Tổng số files | 35+ |
| Directories | 10 |
| ADRs | 6 |
| Module docs | 9 |

### 1.2 Cây thư mục

```
docs/
│
├── 📋 00_DOCUMENTATION_INDEX.md          ← BẠN ĐANG Ở ĐÂY
├── 📋 00_DOCS_MAINTENANCE_GUIDE.md       # Hướng dẫn bảo trì docs
├── 📋 GIT_BRANCHING_GUIDELINES.md        # Quy tắc Git & PR
│
├── 📁 01_DESIGN_PHILOSOPHY/              # Tư duy thiết kế
│   ├── ARCHITECTURE_CONVENTIONS.md       # Tiêu chuẩn OOP (.NET 9 & React 19)
│   ├── DESIGN_PRINCIPLES.md              # SOLID, Clean Architecture, DDD
│   └── PATTERN_CATALOG.md                # Catalog Design Patterns
│
├── 📁 02_STANDARDS/                      # Quy chuẩn coding
│   ├── CODING_STANDARDS_BACKEND.md       # .NET 9 conventions
│   ├── CODING_STANDARDS_FRONTEND.md      # React 19 conventions
│   ├── API_CONVENTIONS.md                # REST API design & versioning
│   ├── DATABASE_CONVENTIONS.md           # EF Core, migrations
│   ├── DOCUMENTATION_STANDARDS.md        # XML Doc / JSDoc / ADR format
│   └── DTO_REFERENCE.md                  # Catalog DTOs
│
├── 📁 03_ARCHITECTURE/                   # Kiến trúc hệ thống
│   ├── SYSTEM_TOPOLOGY.md                # Infrastructure & deployment
│   ├── DOMAIN_MODEL.md                   # Entity relationships & aggregates
│   ├── DEPENDENCY_GRAPH.md               # Service dependencies & DI
│   ├── SECURITY.md                       # STRIDE threat model, GDPR
│   ├── RISK_ASSESSMENT.md                # Risks & constraints
│   ├── STRATEGIC_ROADMAP.md              # Gap analysis & roadmap
│   └── 📁 ADR/                           # Architecture Decision Records
│       ├── ADR_TEMPLATE.md
│       ├── ADR-001_MODULAR_MONOLITH.md
│       ├── ADR-002_TPH_INHERITANCE.md
│       ├── ADR-003_CQRS_MEDIATR.md
│       ├── ADR-004_DUAL_DB_PROVIDER.md
│       ├── ADR-005_JWT_SIGNALR_AUTH.md
│       └── ADR-006_STRATEGY_SMART_COLLECTIONS.md
│
├── 📁 04_MODULES/                        # Domain Modules
│   ├── MODULE_TEMPLATE.md                # Template chuẩn
│   ├── ASSET_MODULE.md                   # 🔴 Core: Asset Management
│   ├── COLLECTION_MODULE.md              # 🔴 Core: Collection Management
│   ├── SMART_COLLECTION_MODULE.md        # 🔴 Core: Smart Collections
│   ├── AUTH_MODULE.md                    # 🟢 Generic: Authentication
│   ├── STORAGE_MODULE.md                 # 🟢 Generic: File Storage
│   ├── PERMISSION_MODULE.md              # 🟢 Generic: RBAC
│   ├── REALTIME_MODULE.md                # 🟢 Generic: SignalR
│   ├── TAG_MODULE.md                     # 🟡 Supporting: Tags
│   └── SEARCH_MODULE.md                  # 🟡 Supporting: Search
│
├── 📁 05_FRONTEND/                       # Frontend Documentation
│   ├── COMPONENT_CATALOG.md              # React components
│   ├── STATE_MANAGEMENT.md               # Context & hooks
│   └── API_LAYER.md                      # API services (OOP)
│
├── 📁 06_OPERATIONS/                     # Vận hành & Triển khai
│   ├── RUNBOOK.md                        # Procedures & rollback
│   ├── TROUBLESHOOTING.md                # Common issues
│   └── INCIDENT_RESPONSE.md              # Failure modes & SLOs
│
├── 📁 07_CHANGELOG/                      # Nhật ký thay đổi
│   ├── CHANGELOG.md                      # Version history
│   ├── TECHNICAL_DEBT.md                 # Tracked debt items
│   └── REFACTOR_LOG.md                   # Refactoring history
│
├── 📁 08_REPORTS/                        # Báo cáo lịch sử (đóng băng)
│   ├── INDEX.md                          # Report registry
│   ├── OOP_ASSESSMENT.md                 # OOP assessment
│   ├── PHASE1_REPORT.md                  # Phase 1 report
│   ├── FIX_REPORT_20260227.md            # Development sessions
│   └── DOCUMENTATION_AUDIT_REPORT.md     # Documentation audit
│
└── GIT_BRANCHING_GUIDELINES.md            # Quy tắc Git workflow
```

> **Note**: Ghi chú thay đổi chi tiết (per-refactor notes) đã được tổng hợp vào `07_CHANGELOG/REFACTOR_LOG.md`.

---

## §2 — Hướng dẫn Đọc

### 2.1 Theo vai trò

| Vai trò | Bắt đầu từ | Tiếp theo |
|---------|-----------|-----------|
| **New Developer** | `RUNBOOK.md` | → `CODING_STANDARDS_*.md` → `ASSET_MODULE.md` |
| **Backend Dev** | `CODING_STANDARDS_BACKEND.md` | → `04_MODULES/*` → `API_CONVENTIONS.md` |
| **Frontend Dev** | `CODING_STANDARDS_FRONTEND.md` | → `05_FRONTEND/*` |
| **Tech Lead** | `SYSTEM_TOPOLOGY.md` | → `DOMAIN_MODEL.md` → `ADR/*` |
| **Architect** | `DESIGN_PRINCIPLES.md` | → `ARCHITECTURE_CONVENTIONS.md` → `STRATEGIC_ROADMAP.md` |

### 2.2 Thứ tự đọc đề xuất (Top 7)

| # | File | Nội dung | Đối tượng |
|---|------|----------|-----------|
| 1 | [ARCHITECTURE_CONVENTIONS.md](01_DESIGN_PHILOSOPHY/ARCHITECTURE_CONVENTIONS.md) | Tư duy OOP & quy ước kiến trúc | Tất cả |
| 2 | [DESIGN_PRINCIPLES.md](01_DESIGN_PHILOSOPHY/DESIGN_PRINCIPLES.md) | SOLID, Clean Architecture | Tất cả |
| 3 | [SYSTEM_TOPOLOGY.md](03_ARCHITECTURE/SYSTEM_TOPOLOGY.md) | Toàn cảnh hệ thống | Tech Lead |
| 4 | [CODING_STANDARDS_BACKEND.md](02_STANDARDS/CODING_STANDARDS_BACKEND.md) | Quy tắc .NET 9 | Backend |
| 5 | [CODING_STANDARDS_FRONTEND.md](02_STANDARDS/CODING_STANDARDS_FRONTEND.md) | Quy tắc React 19 | Frontend |
| 6 | [ASSET_MODULE.md](04_MODULES/ASSET_MODULE.md) | Core module chính | Tất cả |
| 7 | [RUNBOOK.md](06_OPERATIONS/RUNBOOK.md) | Setup & procedures | New members |

---

## §3 — Quy ước Đặt tên

| Loại | Pattern | Ví dụ |
|------|---------|-------|
| Folder | `XX_SNAKE_UPPER/` | `01_DESIGN_PHILOSOPHY/` |
| File | `UPPER_SNAKE_CASE.md` | `ARCHITECTURE_CONVENTIONS.md` |
| ADR | `ADR-NNN_SHORT_TITLE.md` | `ADR-001_MODULAR_MONOLITH.md` |
| Changes | `YYYY-MM-DD_short-name.md` | `2026-03-17_applicationuser_refactor.md` |
| Prefix | 2 digits | `00_`, `01_`, ..., `08_` |

---

## §4 — Phân loại Module

### 4.1 Theo Domain

| Loại | Icon | Mô tả | Modules |
|------|------|-------|---------|
| **Core** | 🔴 | Business logic chính | Asset, Collection, Smart Collection |
| **Generic** | 🟢 | Reusable across domains | Auth, Storage, Permission, Realtime |
| **Supporting** | 🟡 | Hỗ trợ core modules | Tag, Search |

### 4.2 Architecture Decision Records (ADRs)

| ADR | Quyết định | Status |
|-----|-----------|--------|
| [ADR-001](03_ARCHITECTURE/ADR/ADR-001_MODULAR_MONOLITH.md) | Modular Monolith architecture | ✅ Accepted |
| [ADR-002](03_ARCHITECTURE/ADR/ADR-002_TPH_INHERITANCE.md) | TPH inheritance cho Asset types | ✅ Accepted |
| [ADR-003](03_ARCHITECTURE/ADR/ADR-003_CQRS_MEDIATR.md) | CQRS với MediatR | ✅ Accepted |
| [ADR-004](03_ARCHITECTURE/ADR/ADR-004_DUAL_DB_PROVIDER.md) | Dual DB provider (SQLite + PostgreSQL) | ✅ Accepted |
| [ADR-005](03_ARCHITECTURE/ADR/ADR-005_JWT_SIGNALR_AUTH.md) | JWT + SignalR authentication | ✅ Accepted |
| [ADR-006](03_ARCHITECTURE/ADR/ADR-006_STRATEGY_SMART_COLLECTIONS.md) | Strategy pattern cho Smart Collections | ✅ Accepted |

---

## §5 — Tự động hóa Documentation

### 5.1 .NET 9 — XML Documentation

```csharp
/// <summary>
/// Quản lý vòng đời của Asset trong hệ thống.
/// </summary>
/// <remarks>
/// Implements: IAssetService
/// Dependencies: AppDbContext, IStorageService, IThumbnailService
/// Domain: Core (Asset Management)
/// </remarks>
public class AssetService : IAssetService { }
```

### 5.2 React 19 — JSDoc

```javascript
/**
 * @module AssetsApi
 * @extends BaseApiService
 * @description API layer for asset CRUD operations
 * @dependency {TokenManager} tokenManager - JWT token lifecycle
 * @dependency {AxiosInstance} client - HTTP client
 */
export class AssetsApi extends BaseApiService { }
```

### 5.3 Toolchain

| Tool | Mục đích | Command |
|------|----------|---------|
| `docfx` | Sinh API docs từ XML | `docfx build` |
| `jsdoc` | Sinh docs từ JSDoc | `npx jsdoc src/ -r` |
| `swagger` | OpenAPI spec | `/swagger/v1/swagger.json` |

---

## §6 — Lịch sử Migration

> Các file legacy đã được migrate vào cấu trúc mới và archived.

| File gốc | Status | Đã chuyển tới |
|----------|--------|---------------|
| `ARCHITECTURE_REVIEW.md` | ✅ Migrated | DESIGN_PRINCIPLES, RISK_ASSESSMENT, ARCHITECTURE_CONVENTIONS, SYSTEM_TOPOLOGY, DOMAIN_MODEL, TECHNICAL_DEBT, STRATEGIC_ROADMAP, SECURITY, INCIDENT_RESPONSE, API_CONVENTIONS |
| `PROJECT_DOCUMENTATION.md` | ✅ Migrated | SYSTEM_TOPOLOGY, DEPENDENCY_GRAPH, DOMAIN_MODEL, DTO_REFERENCE, 04_MODULES/*, 05_FRONTEND/* |
| `IMPLEMENTATION_GUIDE.md` | ✅ Migrated | RUNBOOK, TROUBLESHOOTING, SECURITY |
| `OOP_ASSESSMENT.md` | ✅ Archived | 08_REPORTS/OOP_ASSESSMENT.md |
| `PHASE1_REPORT.md` | ✅ Archived | 08_REPORTS/PHASE1_REPORT.md |
| `FIX_REPORT_20260227.md` | ✅ Archived | 08_REPORTS/FIX_REPORT_20260227.md |
| `DOCUMENTATION_AUDIT_REPORT.md` | ✅ Archived | 08_REPORTS/DOCUMENTATION_AUDIT_REPORT.md |

---

## §7 — Quick Reference

```
📁 Cấu trúc:
├── 00_*                 → Index & maintenance guides
├── 01_DESIGN_PHILOSOPHY → OOP, SOLID, patterns
├── 02_STANDARDS         → Coding conventions
├── 03_ARCHITECTURE      → System design + ADRs
├── 04_MODULES           → Domain module docs
├── 05_FRONTEND          → React components & state
├── 06_OPERATIONS        → Runbook & troubleshooting
├── 07_CHANGELOG         → Version history & debt
├── 08_REPORTS           → Historical (frozen)
└── CHANGES              → Per-refactor notes
```

---

> **Xem thêm**: [00_DOCS_MAINTENANCE_GUIDE.md](00_DOCS_MAINTENANCE_GUIDE.md) — Hướng dẫn khi nào cập nhật file nào
