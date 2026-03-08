# Visual Asset Hub (VAH)

**Digital Asset Management** — Ứng dụng web quản lý tài nguyên số (ảnh, link, màu sắc) với phân quyền, real-time sync, và giao diện Dark Navy hiện đại.

![.NET 9](https://img.shields.io/badge/.NET-9.0-purple)
![React 19](https://img.shields.io/badge/React-19.2-blue)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-blue)
![Redis](https://img.shields.io/badge/Redis-7-red)
![Docker](https://img.shields.io/badge/Docker-Compose-blue)
![SignalR](https://img.shields.io/badge/SignalR-10.0-green)

---

## Tính năng chính

- **Asset Management** — Upload, tổ chức, tìm kiếm files/links/colors với TPH inheritance (5 asset types)
- **Collection Tree** — Phân cấp parent-child, 3 default collections, drag-and-drop
- **Smart Collections** — 8+ bộ sưu tập ảo tự động (Strategy Pattern: by type, by tag, by date, recent, untagged...)
- **Tags** — Hệ thống many-to-many (normalized dedup), autocomplete, bulk tag, migrate legacy
- **Thumbnails** — Auto-generate 3 sizes (sm/md/lg WebP), ImageSharp 3.1.12 pipeline
- **Real-time Sync** — SignalR WebSocket, user-scoped groups, auto-reconnect
- **Multi-select** — Ctrl+click, Shift+click, bulk delete/move/tag
- **Undo/Redo** — Ctrl+Z / Ctrl+Shift+Z, max 50 history
- **Drag Canvas** — Free positioning trên infinite canvas (PositionX/Y persistence)
- **Color Board** — Tạo và quản lý bảng màu (ColorAsset + ColorGroupAsset)
- **RBAC** — Owner/Editor/Viewer per collection, share bằng email
- **CQRS** — MediatR command/query separation cho Asset module
- **Docker** — 4-service compose (PostgreSQL, Redis, Backend, Frontend)
- **Structured Logging** — Serilog (Console + File rolling daily)
- **Error Handling** — RFC 7807 ProblemDetails via GlobalExceptionHandler, machine-readable `code` extensions (ApiErrors factory)

---

## Tech Stack

| Layer | Công nghệ |
|-------|-----------|
| Backend | ASP.NET Core 9.0, EF Core 9, MediatR 14, JWT + Identity |
| Frontend | React 19.2, Vite 7.3, React Router 7.13, Axios |
| Database | SQLite (dev) / PostgreSQL 17 (prod) — dual-provider |
| Cache | Redis 7 (với in-memory fallback) |
| Real-time | SignalR 10.0 |
| Thumbnails | SixLabors.ImageSharp 3.1.12 |
| Logging | Serilog (Console + File) |
| Deploy | Docker Compose, Nginx |

### Architecture Highlights

| Pattern | Implementation |
|---------|---------------|
| **Modular Monolith** | Vertical slices (Features/Assets/) + Service layer |
| **CQRS** | MediatR commands/queries cho Asset module |
| **TPH Inheritance** | Asset base + 5 subtypes (Image, Link, Color, ColorGroup, Folder) |
| **Factory** | AssetFactory — 8 static creation methods |
| **Strategy** | ISmartCollectionFilter — 5 filter strategies |
| **Observer** | SignalR hub — user-scoped notification groups |
| **Singleton** | TokenManager (frontend) — private #storageKey |
| **Facade** | AssetApplicationService — wraps ISender + IUserContextProvider |

---

## Quick Start

### Docker (khuyến nghị)

```bash
git clone https://github.com/Thang4869/Visual-Asset-Hub.git
cd Visual-Asset-Hub
docker compose up --build -d
```

| Service | URL |
|---------|-----|
| Frontend | http://localhost:3000 |
| Backend API | http://localhost:5027 |
| Swagger | http://localhost:5027/swagger |
| PostgreSQL | localhost:5432 |
| Redis | localhost:6379 |

### Local Development

**Backend (SQLite — no Docker needed):**
```bash
cd VAH.Backend
dotnet restore
dotnet run
# → http://localhost:5027
# → Swagger: http://localhost:5027/swagger
```

**Frontend:**
```bash
cd VAH.Frontend
npm install
npm run dev
# → http://localhost:5173
```

---

## API Endpoints (60 total)

> Chi tiết đầy đủ: [docs/02_STANDARDS/API_CONVENTIONS.md](docs/02_STANDARDS/API_CONVENTIONS.md)

---

## Cấu trúc dự án

```
VAH/
├── docker-compose.yml
├── VAH.sln
├── README.md
│
├── docs/                              # 30+ files — 8 directories
│   ├── 00_DOCUMENTATION_INDEX.md      # Master index & reading order
│   ├── 00_DOCS_MAINTENANCE_GUIDE.md   # Khi nào cập nhật file nào
│   ├── 01_DESIGN_PHILOSOPHY/          # OOP standards, SOLID, pattern catalog
│   ├── 02_STANDARDS/                  # Coding & API conventions
│   ├── 03_ARCHITECTURE/               # Topology, domain model, ADRs
│   ├── 04_MODULES/                    # 9 module docs + template
│   ├── 05_FRONTEND/                   # Components, state, API layer
│   ├── 06_OPERATIONS/                 # Runbook, troubleshooting
│   ├── 07_CHANGELOG/                  # Changelog, tech debt, refactor log
│   └── 08_REPORTS/                    # Historical reports
│
├── VAH.Backend/                       # .NET 9 API
│   ├── Controllers/                   # 15 controllers (incl. abstract base)
│   │   ├── Filters/                   # ValidateBatchFilterAttribute (DRY batch guard)
│   │   └── Requests/                  # Request DTOs
│   ├── Features/                      # Vertical slices (CQRS)
│   │   └── Assets/
│   │       ├── Application/           # IAssetApplicationService (Facade)
│   │       ├── Commands/              # MediatR command records + handlers
│   │       ├── Queries/               # MediatR query records + handlers
│   │       ├── Common/                # Route names
│   │       ├── Contracts/             # Feature-specific DTOs
│   │       └── Infrastructure/        # UserContextProvider, FileMapperService
│   ├── CQRS/Assets/                   # Command/Query records + Handlers
│   ├── Services/                      # 11 service interfaces + implementations
│   ├── Models/                        # 12 files — entities, DTOs, enums, factory
│   ├── Data/                          # EF Core AppDbContext
│   ├── Configuration/                 # AssetOptions
│   ├── Extensions/                    # ServiceCollectionExtensions (6 DI groups)
│   ├── Hubs/                          # AssetHub (SignalR)
│   ├── Middleware/                    # GlobalExceptionHandler (RFC 7807)
│   ├── Exceptions/                    # NotFoundException, ValidationException, AuthContextMissingException
│   └── Migrations/                    # 5 migrations (PostgreSQL + SQLite)
│
└── VAH.Frontend/                      # React 19 SPA
    └── src/
        ├── api/                       # 11 files — BaseApiService + 7 subclasses + TokenManager
        ├── hooks/                     # 11 custom hooks
        ├── components/                # 17 components
        └── context/                   # AppContext + ConfirmContext
```

---

## Tài liệu

> **Full index**: [docs/00_DOCUMENTATION_INDEX.md](docs/00_DOCUMENTATION_INDEX.md)  
> **Maintenance guide**: [docs/00_DOCS_MAINTENANCE_GUIDE.md](docs/00_DOCS_MAINTENANCE_GUIDE.md)

### Đọc nhanh (Top 5)

| # | File | Nội dung |
|---|------|----------|
| 1 | [ARCHITECTURE_CONVENTIONS.md](docs/01_DESIGN_PHILOSOPHY/ARCHITECTURE_CONVENTIONS.md) | OOP standards, SOLID rules, 18 sections |
| 2 | [DESIGN_PRINCIPLES.md](docs/01_DESIGN_PHILOSOPHY/DESIGN_PRINCIPLES.md) | Tại sao chọn kiến trúc này |
| 3 | [ASSET_MODULE.md](docs/04_MODULES/ASSET_MODULE.md) | Core module documentation |
| 4 | [API_CONVENTIONS.md](docs/02_STANDARDS/API_CONVENTIONS.md) | 60 endpoints, HTTP methods, pagination |
| 5 | [DOMAIN_MODEL.md](docs/03_ARCHITECTURE/DOMAIN_MODEL.md) | Entity relationships & aggregates |

### Architecture Decision Records (ADRs)

| ADR | Quyết định |
|-----|-----------|
| [ADR-001](docs/03_ARCHITECTURE/ADR/ADR-001_MODULAR_MONOLITH.md) | Modular Monolith architecture |
| [ADR-002](docs/03_ARCHITECTURE/ADR/ADR-002_TPH_INHERITANCE.md) | TPH inheritance for Asset types |
| [ADR-003](docs/03_ARCHITECTURE/ADR/ADR-003_CQRS_MEDIATR.md) | CQRS with MediatR |
| [ADR-004](docs/03_ARCHITECTURE/ADR/ADR-004_DUAL_DB_PROVIDER.md) | Dual DB provider (SQLite + PostgreSQL) |
| [ADR-005](docs/03_ARCHITECTURE/ADR/ADR-005_JWT_SIGNALR_AUTH.md) | JWT + SignalR authentication |
| [ADR-006](docs/03_ARCHITECTURE/ADR/ADR-006_STRATEGY_SMART_COLLECTIONS.md) | Strategy pattern for Smart Collections |

### Legacy (archived)

| File | Nội dung |
|------|----------|
| [docs/ARCHITECTURE_REVIEW.md](docs/ARCHITECTURE_REVIEW.md) | Full architecture assessment (v10.0) |
| [docs/OOP_ASSESSMENT.md](docs/OOP_ASSESSMENT.md) | OOP assessment & refactor progress |
| [docs/IMPLEMENTATION_GUIDE.md](docs/IMPLEMENTATION_GUIDE.md) | Setup & deployment guide |
| [docs/PROJECT_DOCUMENTATION.md](docs/PROJECT_DOCUMENTATION.md) | Technical documentation (pre-CQRS) |
| [docs/PHASE1_REPORT.md](docs/PHASE1_REPORT.md) | Phase 1 completion report |
| [docs/FIX_REPORT_20260227.md](docs/FIX_REPORT_20260227.md) | Fix report 2026-02-27 |

---

## License

MIT
