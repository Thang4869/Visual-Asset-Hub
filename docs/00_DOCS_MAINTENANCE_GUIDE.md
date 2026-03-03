# DOCS MAINTENANCE GUIDE — Khi nào cập nhật file nào

> **Mục đích**: Giúp developer biết chính xác phải cập nhật docs nào khi thêm feature, fix bug, refactor, hoặc deploy.  
> **Last Updated**: 2026-03-03

---

## §1 — Phân loại docs theo tần suất cập nhật

### 🔴 Tier 1 — CẬP NHẬT MỖI FEATURE / BUG FIX

Những file này **gần như chắc chắn** phải sửa mỗi khi code thay đổi:

| File | Khi nào cập nhật | Nội dung cần thay đổi |
|------|-----------------|----------------------|
| [CHANGELOG.md](07_CHANGELOG/CHANGELOG.md) | Mỗi PR / mỗi version | Thêm entry vào `[Unreleased]` section |
| [TECHNICAL_DEBT.md](07_CHANGELOG/TECHNICAL_DEBT.md) | Khi phát hiện hoặc resolve debt | Thêm/cập nhật Status row |
| [API_CONVENTIONS.md](02_STANDARDS/API_CONVENTIONS.md) | Khi thêm/sửa/xóa endpoint | Cập nhật endpoint table (§3) |

### 🟠 Tier 2 — CẬP NHẬT KHI MODULE LIÊN QUAN THAY ĐỔI

Chỉ sửa file tương ứng với module bạn đang code:

| File | Trigger cập nhật |
|------|-----------------|
| [ASSET_MODULE.md](04_MODULES/ASSET_MODULE.md) | Thêm/sửa Asset CRUD, upload, duplicate, CQRS command/query |
| [COLLECTION_MODULE.md](04_MODULES/COLLECTION_MODULE.md) | Thêm/sửa Collection CRUD, nesting, items |
| [TAG_MODULE.md](04_MODULES/TAG_MODULE.md) | Thêm/sửa Tag CRUD, asset-tag operations |
| [SEARCH_MODULE.md](04_MODULES/SEARCH_MODULE.md) | Thêm/sửa search logic, filters |
| [PERMISSION_MODULE.md](04_MODULES/PERMISSION_MODULE.md) | Thêm/sửa RBAC, sharing |
| [SMART_COLLECTION_MODULE.md](04_MODULES/SMART_COLLECTION_MODULE.md) | Thêm filter strategy mới |
| [AUTH_MODULE.md](04_MODULES/AUTH_MODULE.md) | Thêm/sửa auth flow, JWT, Identity config |
| [STORAGE_MODULE.md](04_MODULES/STORAGE_MODULE.md) | Thêm/sửa file upload, thumbnails, storage provider |
| [REALTIME_MODULE.md](04_MODULES/REALTIME_MODULE.md) | Thêm/sửa SignalR events, hub methods |
| [COMPONENT_CATALOG.md](05_FRONTEND/COMPONENT_CATALOG.md) | Thêm/sửa/xóa React component |
| [STATE_MANAGEMENT.md](05_FRONTEND/STATE_MANAGEMENT.md) | Thêm/sửa hook, context, state flow |
| [API_LAYER.md](05_FRONTEND/API_LAYER.md) | Thêm/sửa API service class hoặc method |

### 🟡 Tier 3 — CẬP NHẬT KHI KIẾN TRÚC THAY ĐỔI

Chỉ sửa khi thay đổi cấu trúc hệ thống (không phải feature thường):

| File | Trigger cập nhật |
|------|-----------------|
| [DOMAIN_MODEL.md](03_ARCHITECTURE/DOMAIN_MODEL.md) | Thêm entity mới, đổi relationship, đổi aggregate |
| [DEPENDENCY_GRAPH.md](03_ARCHITECTURE/DEPENDENCY_GRAPH.md) | Thêm service mới vào DI, đổi dependency chain |
| [PATTERN_CATALOG.md](01_DESIGN_PHILOSOPHY/PATTERN_CATALOG.md) | Áp dụng design pattern mới vào codebase |
| [REFACTOR_LOG.md](07_CHANGELOG/REFACTOR_LOG.md) | Sau mỗi refactoring có before/after rõ ràng |
| [DESIGN_PRINCIPLES.md](01_DESIGN_PHILOSOPHY/DESIGN_PRINCIPLES.md) | Thêm aggregate root (§4.2), fix violation (§3.3) |
| [TROUBLESHOOTING.md](06_OPERATIONS/TROUBLESHOOTING.md) | Phát hiện issue mới + solution |

### 🟢 Tier 4 — GẦN NHƯ CỐ ĐỊNH — Chỉ sửa khi có thay đổi kiến trúc lớn

| File | Chỉ sửa khi |
|------|------------|
| [ARCHITECTURE_CONVENTIONS.md](01_DESIGN_PHILOSOPHY/ARCHITECTURE_CONVENTIONS.md) | Thêm OOP rule mới (§19 Testing, §20 Deploy) — xem roadmap đã đề xuất |
| [CODING_STANDARDS_BACKEND.md](02_STANDARDS/CODING_STANDARDS_BACKEND.md) | Đổi .NET version hoặc thêm convention mới |
| [CODING_STANDARDS_FRONTEND.md](02_STANDARDS/CODING_STANDARDS_FRONTEND.md) | Đổi React version hoặc thêm convention mới |
| [DATABASE_CONVENTIONS.md](02_STANDARDS/DATABASE_CONVENTIONS.md) | Đổi DB provider, thêm migration convention |
| [DOCUMENTATION_STANDARDS.md](02_STANDARDS/DOCUMENTATION_STANDARDS.md) | Đổi doc toolchain |
| [SYSTEM_TOPOLOGY.md](03_ARCHITECTURE/SYSTEM_TOPOLOGY.md) | Thêm/bỏ service trong Docker Compose, đổi infra |
| [RUNBOOK.md](06_OPERATIONS/RUNBOOK.md) | Đổi deployment process, thêm environment |

### 🔵 Tier 5 — ĐÓNG BĂNG VĨNH VIỄN — Không bao giờ sửa

| File | Lý do |
|------|-------|
| [ADR_TEMPLATE.md](03_ARCHITECTURE/ADR/ADR_TEMPLATE.md) | Template cố định |
| [MODULE_TEMPLATE.md](04_MODULES/MODULE_TEMPLATE.md) | Template cố định |
| [ADR-001_MODULAR_MONOLITH.md](03_ARCHITECTURE/ADR/ADR-001_MODULAR_MONOLITH.md) | ADR = immutable record. Nếu đổi → viết ADR mới supersede |
| [ADR-002_TPH_INHERITANCE.md](03_ARCHITECTURE/ADR/ADR-002_TPH_INHERITANCE.md) | (như trên) |
| [ADR-003_CQRS_MEDIATR.md](03_ARCHITECTURE/ADR/ADR-003_CQRS_MEDIATR.md) | (như trên) |
| [ADR-004_DUAL_DB_PROVIDER.md](03_ARCHITECTURE/ADR/ADR-004_DUAL_DB_PROVIDER.md) | (như trên) |
| [ADR-005_JWT_SIGNALR_AUTH.md](03_ARCHITECTURE/ADR/ADR-005_JWT_SIGNALR_AUTH.md) | (như trên) |
| [ADR-006_STRATEGY_SMART_COLLECTIONS.md](03_ARCHITECTURE/ADR/ADR-006_STRATEGY_SMART_COLLECTIONS.md) | (như trên) |
| [00_DOCUMENTATION_INDEX.md](00_DOCUMENTATION_INDEX.md) | Chỉ sửa nếu thêm directory mới (rất hiếm) |
| `08_REPORTS/*` (tất cả) | Historical snapshots — đã đóng băng |
| `docs/*.md` (root-level legacy) | Đã thay thế bởi hệ thống mới |

---

## §2 — Checklist theo loại thay đổi

### Khi THÊM FEATURE MỚI (ví dụ: thêm Workspace module)

```
□ 07_CHANGELOG/CHANGELOG.md                 → Thêm entry [Unreleased]
□ 04_MODULES/{MODULE}_MODULE.md             → Tạo mới hoặc cập nhật module doc
□ 02_STANDARDS/API_CONVENTIONS.md           → Thêm endpoints mới vào bảng
□ 03_ARCHITECTURE/DOMAIN_MODEL.md           → Thêm entity nếu có entity mới
□ 03_ARCHITECTURE/DEPENDENCY_GRAPH.md       → Thêm service nếu có service mới
□ 05_FRONTEND/*                             → Cập nhật nếu có component/hook/API mới
□ 01_DESIGN_PHILOSOPHY/DESIGN_PRINCIPLES.md → Thêm aggregate root nếu có (§4.2)
```

### Khi FIX BUG

```
□ 07_CHANGELOG/CHANGELOG.md         → Thêm entry Fixed trong [Unreleased]
□ 06_OPERATIONS/TROUBLESHOOTING.md  → Thêm issue + solution nếu bug phổ biến
```

### Khi REFACTOR

```
□ 07_CHANGELOG/REFACTOR_LOG.md          → Ghi before/after
□ 07_CHANGELOG/TECHNICAL_DEBT.md        → Cập nhật status debt item đã resolve
□ 04_MODULES/{MODULE}_MODULE.md         → Cập nhật nếu interface/flow thay đổi
□ 03_ARCHITECTURE/DEPENDENCY_GRAPH.md   → Cập nhật nếu DI thay đổi
```

### Khi THÊM DESIGN PATTERN MỚI

```
□ 01_DESIGN_PHILOSOPHY/PATTERN_CATALOG.md   → Thêm row vào catalog
□ 03_ARCHITECTURE/ADR/ADR-NNN_*.md          → Tạo ADR mới nếu là kiến trúc decision
```

### Khi THÊM MIGRATION / ĐỔI SCHEMA

```
□ 03_ARCHITECTURE/DOMAIN_MODEL.md       → Cập nhật ER diagram, table mapping
□ 04_MODULES/{MODULE}_MODULE.md         → Cập nhật domain model section
□ 02_STANDARDS/DATABASE_CONVENTIONS.md  → Chỉ nếu thêm convention mới
```

### Khi DEPLOY

```
□ 07_CHANGELOG/CHANGELOG.md → Chuyển [Unreleased] → [x.y.z] với date
□ 06_OPERATIONS/RUNBOOK.md  → Cập nhật nếu process thay đổi
```

---

## §3 — Quy tắc bảo trì

### 3.1 Last Updated Rule `[MUST]`

Mỗi file docs có header `> **Last Updated**: YYYY-MM-DD`. Khi sửa file, **PHẢI** cập nhật ngày này.

### 3.2 Scope Discipline `[MUST]`

**KHÔNG SỬA** file docs ngoài scope thay đổi. Ví dụ:
- Fix bug trong `TagService` → chỉ sửa `TAG_MODULE.md` + `CHANGELOG.md`
- KHÔNG sửa `STORAGE_MODULE.md` chỉ vì "tiện tay"

### 3.3 ADR Immutability `[MUST]`

ADR đã accepted **KHÔNG BAO GIỜ** sửa nội dung. Nếu quyết định cũ bị thay thế:
1. Tạo ADR mới (ADR-007, 008, ...)
2. Trong ADR mới ghi: `Supersedes: ADR-00X`
3. Trong ADR cũ chỉ sửa duy nhất 1 dòng: `Status: Superseded by ADR-00Y`

### 3.4 Template Immutability `[MUST]`

`ADR_TEMPLATE.md` và `MODULE_TEMPLATE.md` là templates cố định. Nếu muốn đổi format, tạo `_TEMPLATE_V2.md`.

---

## §4 — Thống kê tổng quan

```
Tổng files docs mới:                30  files
├── 🔴 Tier 1 (mỗi feature):        3   files   (10%)
├── 🟠 Tier 2 (theo module):        12  files   (40%)
├── 🟡 Tier 3 (kiến trúc):          6   files   (20%)
├── 🟢 Tier 4 (gần cố định):        7   files   (23%)
└── 🔵 Tier 5 (đóng băng):          10+ files

Trung bình mỗi feature mới:     sửa 3-5 files docs
Trung bình mỗi bug fix:         sửa 1-2 files docs
Trung bình mỗi refactor:        sửa 2-4 files docs
```

---

> **Document End**
