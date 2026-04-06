# Hướng dẫn Phong cách Tài liệu (Documentation Style Guide)

> **Mục đích**: Quy chuẩn format và style để đảm bảo consistency toàn bộ docs  
> **Last Updated**: 2026-04-06

---

## §1 — Format Chuẩn

### 1.1 Header Template

```markdown
# Tiêu đề Tiếng Việt (English Title)

> **Mục đích**: [Mô tả ngắn gọn bằng tiếng Việt]  
> **Last Updated**: YYYY-MM-DD  
> **[Optional]**: Additional metadata

---
```

### 1.2 Section Structure

```markdown
## §1 — Tên Section Tiếng Việt

### 1.1 Subsection

Nội dung...

---

## §2 — Section Tiếp Theo

---

> **Document End**
```

---

## §2 — Language Guidelines

### 2.1 Vietnamese Content
- **Titles**: Tiếng Việt với (English) trong ngoặc
- **Section headers**: Tiếng Việt
- **Descriptions**: Tiếng Việt
- **Table headers**: Tiếng Việt

### 2.2 English Content (Preserved)
- **Code examples**: English
- **Class/interface names**: English  
- **API endpoints**: English
- **File paths**: English
- **Technical terms**: English (class, method, property names)

### 2.3 Example

```markdown
## §3 — Entity Design (Thiết kế Entity)

Class `AssetService` implements interface `IAssetService`:

```csharp
public class AssetService : IAssetService
{
    public async Task<AssetResponseDto> CreateAsync(CreateAssetDto dto)
    {
        // Implementation...
    }
}
```

Endpoint tương ứng: `POST /api/v1/assets`
```

---

## §3 — Formatting Rules

### 3.1 Tables

```markdown
| Vietnamese Header | English Header | Status |
|------------------|----------------|--------|
| Nội dung | Content | ✅ |
```

### 3.2 Code Blocks

- Use `csharp`, `javascript`, `json`, `bash` for syntax highlighting
- Include comments in English
- Use realistic examples (not placeholder text)

### 3.3 Cross-references

```markdown
Xem thêm: [Module Documentation](04_MODULES/ASSET_MODULE.md)
Chi tiết: [API Conventions](02_STANDARDS/API_CONVENTIONS.md)
```

---

## §4 — Icons & Visual Cues

| Icon | Meaning | Usage |
|------|---------|-------|
| ✅ | Completed/Working | Status indicators |
| ❌ | Error/Problem | Issues |
| ⚠️ | Warning | Caveats |
| 🔴 | Core/Critical | Module classification |
| 🟡 | Supporting | Module classification |
| 🟢 | Generic | Module classification |
| 🔵 | Frozen/Archive | Report status |
| 📁 | Directory | File structure |
| 📋 | Document/File | File structure |

---

## §5 — Common Patterns

### 5.1 Module Documentation Pattern

```markdown
# [Module Name] Module

> **Domain**: Core | Supporting | Generic  
> **Status**: Active | Planned | Deprecated  
> **Last Updated**: YYYY-MM-DD

## §1 — Mục đích (Purpose)
## §2 — Kiến trúc (Architecture) 
## §3 — Interfaces chính (Key Interfaces)
## §4 — Domain Entities
## §5 — API Endpoints
```

### 5.2 Standards Documentation Pattern

```markdown  
# Tiêu chuẩn [Area] ([Area] Standards)

> **Mục đích**: [Vietnamese description]  
> **Last Updated**: YYYY-MM-DD

## §1 — Tổng quan (Overview)
## §2 — Quy tắc (Rules)
## §3 — Ví dụ (Examples)
```

---

## §6 — Quality Checklist

Before committing documentation changes:

- [ ] **Vietnamese title** with English subtitle
- [ ] **Last Updated**: Current date (YYYY-MM-DD)
- [ ] **§ numbering** for sections
- [ ] **Technical content** in English
- [ ] **Cross-references** working
- [ ] **Document End** marker
- [ ] **Consistent terminology** across docs

---

## §7 — Terminology Dictionary

| Vietnamese | English | Context |
|-----------|---------|---------|
| Mục đích | Purpose | File headers |
| Tổng quan | Overview | Section headers |
| Kiến trúc | Architecture | Technical sections |
| Quy tắc | Rules | Standards docs |
| Ví dụ | Examples | Code sections |
| Phụ thuộc | Dependencies | Module relationships |
| Thực thi | Implementation | Technical details |

---

> **Document End**