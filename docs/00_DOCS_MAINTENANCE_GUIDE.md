# Hướng dẫn Bảo trì Tài liệu (Docs Maintenance Guide)

> **Mục đích**: Hướng dẫn developer biết chính xác file nào cần cập nhật khi thay đổi mã nguồn (feature, bug fix, refactor, deploy).  
> **Last Updated**: 2026-04-06

---

## §1 — Phân loại tài liệu theo tần suất cập nhật

Mỗi file docs được gán vào một tier để xác định khi nào cần sửa.

### 🔴 Tier 1 — CẬP NHẬT GẦN NHƯ MỖI LẦN (Feature / Bug fix)
Những file này thường phải cập nhật khi thay đổi mã nguồn liên quan:

- 07_CHANGELOG/CHANGELOG.md — Thêm entry vào mục [Unreleased] cho mọi PR lớn/nhỏ.
- 07_CHANGELOG/TECHNICAL_DEBT.md — Ghi/điều chỉnh status technical debt khi phát hiện hoặc resolve.
- 07_CHANGELOG/REFACTOR_LOG.md — Ghi trước/sau (before/after) cho mọi refactor có ý nghĩa.
- 02_STANDARDS/API_CONVENTIONS.md — Cập nhật khi thêm/sửa/xóa endpoint (bảng endpoints).

---

### 🟠 Tier 2 — CẬP NHẬT KHI MODULE LIÊN QUAN THAY ĐỔI
Chỉ sửa các tài liệu thuộc module bạn thay đổi:

- 04_MODULES/ASSET_MODULE.md — Asset CRUD, upload, CQRS, thumbnails...
- 04_MODULES/COLLECTION_MODULE.md — Collection hierarchy, drag/drop, sharing
- 04_MODULES/TAG_MODULE.md — Tag model, migrations, bulk-tag
- 04_MODULES/SEARCH_MODULE.md — Search filters, scoring, index mapping
- 04_MODULES/PERMISSION_MODULE.md — RBAC rules, sharing by email
- 04_MODULES/SMART_COLLECTION_MODULE.md — Thêm strategy mới
- 04_MODULES/AUTH_MODULE.md — JWT/Identity changes
- 04_MODULES/STORAGE_MODULE.md — Storage providers, thumbnail pipeline
- 04_MODULES/REALTIME_MODULE.md — SignalR hub, event contract
- 05_FRONTEND/COMPONENT_CATALOG.md, STATE_MANAGEMENT.md, API_LAYER.md — Frontend changes

---

### 🟡 Tier 3 — CHỈ KHI KIẾN TRÚC THAY ĐỔI
Sửa khi có thay đổi hệ thống lớn (entity, aggregate, DI):

- 03_ARCHITECTURE/DOMAIN_MODEL.md
- 03_ARCHITECTURE/DEPENDENCY_GRAPH.md
- 01_DESIGN_PHILOSOPHY/PATTERN_CATALOG.md
- 07_CHANGELOG/REFACTOR_LOG.md (ghi before/after)
- 06_OPERATIONS/TROUBLESHOOTING.md (nếu phát sinh issue phổ biến)

---

### 🟢 Tier 4 — GẦN NHƯ CỐ ĐỊNH (Chỉ khi có thay đổi lớn)
Chỉ cập nhật khi đổi quy ước toàn hệ thống hoặc infra:

- 01_DESIGN_PHILOSOPHY/ARCHITECTURE_CONVENTIONS.md
- 02_STANDARDS/CODING_STANDARDS_BACKEND.md
- 02_STANDARDS/CODING_STANDARDS_FRONTEND.md
- 02_STANDARDS/DATABASE_CONVENTIONS.md
- 02_STANDARDS/DOCUMENTATION_STANDARDS.md
- 03_ARCHITECTURE/SYSTEM_TOPOLOGY.md
- 06_OPERATIONS/RUNBOOK.md

---

### 🔵 Tier 5 — ĐÓNG BĂNG (Không sửa, trừ khi có lý do rất đặc biệt)
Các file làm nền tảng, template hoặc hồ sơ lịch sử — hiếm khi thay đổi:

- 03_ARCHITECTURE/ADR/ADR_TEMPLATE.md
- 04_MODULES/MODULE_TEMPLATE.md
- ADRs đã accepted (ADR-001 ... ADR-006) — nếu superseded thì thêm ADR mới
- 08_REPORTS/* (báo cáo lịch sử)
- 00_DOCUMENTATION_INDEX.md (chỉ sửa khi thêm folder mới)
- GIT_BRANCHING_GUIDELINES.md (thay đổi Git workflow là sự kiện lớn)

---

## §2 — Checklist theo loại thay đổi (Quick-check)

Khi thực hiện thay đổi, dùng checklist dưới đây để biết file nào cần cập nhật.

Khi THÊM FEATURE MỚI:

- [ ] 07_CHANGELOG/CHANGELOG.md → Thêm entry ở [Unreleased]
- [ ] 04_MODULES/{MODULE}_MODULE.md → Tạo/cập nhật tài liệu module
- [ ] 02_STANDARDS/API_CONVENTIONS.md → Cập nhật endpoints (nếu cần)
- [ ] 03_ARCHITECTURE/DOMAIN_MODEL.md → Cập entity nếu có
- [ ] 03_ARCHITECTURE/DEPENDENCY_GRAPH.md → Cập service nếu DI thay đổi
- [ ] 05_FRONTEND/* → Cập nhật component/hook/API
- [ ] 01_DESIGN_PHILOSOPHY/DESIGN_PRINCIPLES.md → Nếu thay đổi reasoning

Khi FIX BUG:

- [ ] 07_CHANGELOG/CHANGELOG.md → Thêm mục Fixed
- [ ] 06_OPERATIONS/TROUBLESHOOTING.md → Thêm issue + giải pháp nếu phổ biến

Khi REFACTOR:

- [ ] 07_CHANGELOG/REFACTOR_LOG.md → Ghi trước/sau (before/after)
- [ ] 07_CHANGELOG/TECHNICAL_DEBT.md → Cập nhật trạng thái debt liên quan
- [ ] 04_MODULES/{MODULE}_MODULE.md → Cập nhật interface/flow khi thay đổi
- [ ] 03_ARCHITECTURE/DEPENDENCY_GRAPH.md → Nếu DI thay đổi

Khi DEPLOY (release):

- [ ] 07_CHANGELOG/CHANGELOG.md → Chuyển [Unreleased] → [x.y.z] với ngày
- [ ] 06_OPERATIONS/RUNBOOK.md → Cập nhật nếu quy trình deploy thay đổi

---

## §3 — Nguyên tắc bắt buộc (Rules)

1. Last Updated `[MUST]`

Mỗi file docs PHẢI có header `> **Last Updated**: YYYY-MM-DD`. Khi sửa file, cập nhật ngày này.

2. Scope Discipline `[MUST]`

Chỉ sửa các file trong phạm vi thay đổi. Tránh "scope creep" — đừng sửa tài liệu khác chỉ vì "tiện tay".

3. ADR Immutability `[MUST]`

ADR đã accepted KHÔNG BAO GIỜ sửa nội dung. Quy trình thay thế:

- Tạo ADR mới (ADR-00N) mô tả quyết định mới và ghi `Supersedes: ADR-00X`.
- Trong ADR cũ chỉ ghi thêm dòng `Status: Superseded by ADR-00N`.

4. Template Immutability `[MUST]`

Nếu cần thay đổi template chuẩn (ADR_TEMPLATE.md, MODULE_TEMPLATE.md), tạo phiên bản mới `_TEMPLATE_V2.md` và migrate từ từ.

---

## §4 — Thống kê & hướng dẫn vận hành nhẹ

- Tổng tài liệu hiện tại: ~53 files (cập nhật khi có thay đổi lớn)
- Khi thực hiện refactor có ảnh hưởng phạm vi lớn, **ghi vào 07_CHANGELOG/REFACTOR_LOG.md** để mô tả ngắn gọn mục đích, phạm vi và migration steps.

Gợi ý vận hành:

- Trước merge PR lớn: chạy checklist docs (các item ở §2).
- Sau merge: nếu có thay đổi docs, cập nhật `07_CHANGELOG/CHANGELOG.md`.

---

## §5 — Automation & Validation

### 5.1 Documentation Quality Checklist

Trước khi merge PR có thay đổi docs, chạy validation:

```bash
# Kiểm tra consistency
docs/.validate-docs.md

# Hoặc manual check:
- [ ] Headers có "Last Updated: YYYY-MM-DD"
- [ ] Vietnamese titles (trừ templates)
- [ ] § section numbering
- [ ] "Document End" markers
- [ ] Cross-references chính xác
```

### 5.2 Pre-commit Hook (Đề xuất)

```bash
#!/bin/sh
# .git/hooks/pre-commit
echo "Validating documentation..."
if grep -L "Document End" docs/**/*.md | grep -v "08_REPORTS" | grep -q .; then
  echo "❌ Some docs missing 'Document End' marker"
  exit 1
fi
echo "✅ Documentation validation passed"
```

---

## §6 — Tóm tắt ngắn (Quick Reference)

- Tier 1 → cập nhật mọi PR liên quan
- Tier 2 → chỉ sửa module liên quan
- Tier 3 → sửa khi kiến trúc thay đổi
- Tier 4 → sửa khi thay đổi quy ước hệ thống
- Tier 5 → đóng băng, hiếm khi sửa

**Validation**: Check `.validate-docs.md` trước merge

---

> **Document End**

