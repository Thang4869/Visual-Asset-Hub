# 📚 VAH — Documentation Index

> **Visual Asset Hub** — Digital Asset Management Platform  
> Stack: .NET 9 · React 19 · PostgreSQL 17 · Redis  
> Last Updated: 2026-03-02  
> **Status**: ✅ All 30+ documentation files created and verified against source code

---

## Documentation Tree

```
docs/
│
├── 00_DOCUMENTATION_INDEX.md              ← BẠN ĐANG Ở ĐÂY
│
├── 01_DESIGN_PHILOSOPHY/                  # Tư duy thiết kế & Quy ước kiến trúc
│   ├── ARCHITECTURE_CONVENTIONS.md        # Tiêu chuẩn OOP cho .NET 9 & React 19
│   ├── DESIGN_PRINCIPLES.md              # SOLID, Clean Architecture, DDD rationale
│   └── PATTERN_CATALOG.md               # Catalog tất cả Design Patterns đang dùng
│
├── 02_STANDARDS/                          # Quy định chung (Coding Standards)
│   ├── CODING_STANDARDS_BACKEND.md       # .NET 9 coding conventions & XML Documentation
│   ├── CODING_STANDARDS_FRONTEND.md      # React 19 conventions & JSDoc
│   ├── API_CONVENTIONS.md                # REST API design rules & versioning
│   ├── DATABASE_CONVENTIONS.md           # EF Core, migrations, naming conventions
│   └── DOCUMENTATION_STANDARDS.md        # XML Doc / JSDoc / ADR format guide
│
├── 03_ARCHITECTURE/                       # Kiến trúc hệ thống (System-level)
│   ├── ARCHITECTURE_REVIEW.md            # [Existing] Full architecture review
│   ├── SYSTEM_TOPOLOGY.md                # Infrastructure & deployment topology
│   ├── DOMAIN_MODEL.md                   # Entity relationships, aggregates, invariants
│   ├── DEPENDENCY_GRAPH.md               # Service dependency matrix & DI registration
│   └── ADR/                              # Architecture Decision Records
│       ├── ADR_TEMPLATE.md
│       ├── ADR-001_MODULAR_MONOLITH.md
│       ├── ADR-002_TPH_INHERITANCE.md
│       ├── ADR-003_CQRS_MEDIATR.md
│       ├── ADR-004_DUAL_DB_PROVIDER.md
│       ├── ADR-005_JWT_SIGNALR_AUTH.md
│       └── ADR-006_STRATEGY_SMART_COLLECTIONS.md
│
├── 04_MODULES/                            # Chi tiết từng Module (Domain Modules)
│   ├── MODULE_TEMPLATE.md                # Template chuẩn cho mô tả module
│   ├── ASSET_MODULE.md                   # Core: Asset Management (CRUD, Upload, Types)
│   ├── COLLECTION_MODULE.md              # Core: Collection Management
│   ├── AUTH_MODULE.md                    # Generic: Authentication & Authorization
│   ├── STORAGE_MODULE.md                 # Generic: File Storage Abstraction
│   ├── TAG_MODULE.md                     # Supporting: Tag System
│   ├── SEARCH_MODULE.md                  # Supporting: Search & Filtering
│   ├── PERMISSION_MODULE.md              # Generic: RBAC Permissions
│   ├── SMART_COLLECTION_MODULE.md        # Core: Smart Collections (Strategy Pattern)
│   └── REALTIME_MODULE.md               # Generic: SignalR Real-time Sync
│
├── 05_FRONTEND/                           # Frontend-specific documentation
│   ├── COMPONENT_CATALOG.md              # All React components & responsibilities
│   ├── STATE_MANAGEMENT.md               # Context, hooks architecture
│   └── API_LAYER.md                      # Class-based API services (OOP)
│
├── 06_OPERATIONS/                         # Vận hành & Triển khai
│   ├── IMPLEMENTATION_GUIDE.md           # [Existing] Setup & deployment guide
│   ├── RUNBOOK.md                        # Operational procedures & playbooks
│   └── TROUBLESHOOTING.md               # Common issues & resolutions
│
├── 07_CHANGELOG/                          # Nhật ký thay đổi
│   ├── CHANGELOG.md                      # Version history (Keep a Changelog format)
│   ├── TECHNICAL_DEBT.md                 # Tracked debt items & prioritization
│   └── REFACTOR_LOG.md                   # Completed refactorings & impact analysis
│
└── 08_REPORTS/                            # Báo cáo lịch sử (Historical Reports)
    ├── INDEX.md                          # Report registry & migration status
    ├── OOP_ASSESSMENT.md                 # [Legacy] OOP assessment results
    ├── PHASE1_REPORT.md                  # [Legacy] Phase 1 completion report
    ├── FIX_REPORT_20260227.md            # [Legacy] Fix report
    └── PROJECT_DOCUMENTATION.md          # [Legacy] Technical documentation
```

---

## Thứ tự đọc đề xuất (Recommended Reading Order)

| # | File | Mục đích | Đối tượng |
|---|------|----------|-----------|
| 1 | `01_DESIGN_PHILOSOPHY/ARCHITECTURE_CONVENTIONS.md` | Hiểu tư duy OOP & quy ước kiến trúc | Tất cả developers |
| 2 | `01_DESIGN_PHILOSOPHY/DESIGN_PRINCIPLES.md` | Hiểu tại sao chọn SOLID, Clean Arch | Tất cả developers |
| 3 | `03_ARCHITECTURE/ARCHITECTURE_REVIEW.md` | Toàn cảnh kiến trúc hệ thống | Tech Lead, Architect |
| 4 | `02_STANDARDS/CODING_STANDARDS_BACKEND.md` | Quy tắc viết code .NET 9 | Backend developers |
| 5 | `02_STANDARDS/CODING_STANDARDS_FRONTEND.md` | Quy tắc viết code React 19 | Frontend developers |
| 6 | `04_MODULES/ASSET_MODULE.md` | Core module — bắt đầu từ đây | Tất cả developers |
| 7 | `06_OPERATIONS/IMPLEMENTATION_GUIDE.md` | Setup local dev environment | New team members |

---

## Quy ước đặt tên (Naming Conventions)

| Quy tắc | Ví dụ |
|---------|-------|
| Folder: `XX_SNAKE_UPPER` | `01_DESIGN_PHILOSOPHY/` |
| File: `UPPER_SNAKE_CASE.md` | `ARCHITECTURE_CONVENTIONS.md` |
| ADR: `ADR-NNN_SHORT_TITLE.md` | `ADR-001_MODULAR_MONOLITH.md` |
| Prefix số thứ tự: 2 digits | `00_`, `01_`, ..., `08_` |

---

## Automation & Self-Updating Documentation

### .NET 9 — XML Documentation
```csharp
// Mọi public interface, class, method PHẢI có XML doc
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

### React 19 — JSDoc
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

### Toolchain tự động sinh docs
| Tool | Mục đích | Command |
|------|----------|---------|
| `dotnet tool docfx` | Sinh API docs từ XML comments | `docfx build` |
| `jsdoc` | Sinh docs từ JSDoc comments | `npx jsdoc src/ -r` |
| `swagger` | OpenAPI spec tự động | `/swagger/v1/swagger.json` |

---

## File Migration Plan (Existing → New Structure)

| File hiện tại | Vị trí mới |
|--------------|------------|
| `docs/ARCHITECTURE_REVIEW.md` | `docs/03_ARCHITECTURE/ARCHITECTURE_REVIEW.md` |
| `docs/OOP_ASSESSMENT.md` | `docs/08_REPORTS/OOP_ASSESSMENT.md` |
| `docs/IMPLEMENTATION_GUIDE.md` | `docs/06_OPERATIONS/IMPLEMENTATION_GUIDE.md` |
| `docs/PROJECT_DOCUMENTATION.md` | `docs/08_REPORTS/PROJECT_DOCUMENTATION.md` |
| `docs/PHASE1_REPORT.md` | `docs/08_REPORTS/PHASE1_REPORT.md` |
| `docs/FIX_REPORT_20260227.md` | `docs/08_REPORTS/FIX_REPORT_20260227.md` |
