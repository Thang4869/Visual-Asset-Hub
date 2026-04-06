# Tiêu Chuẩn Tài Liệu (Documentation Standards — XML Doc, JSDoc & ADR Format)

> **Mục đích**: Định nghĩa quy tắc viết tài liệu code cho backend và frontend  
> **Last Updated**: 2026-04-06

---

## §1 — XML Documentation (.NET 9)

### Phạm Vi Bắt Buộc

| Thành phần | Tags Bắt Buộc | Mức độ |
|------------|---------------|--------|
| `public interface` | `<summary>`, `<remarks>` (Domain, Pattern, Implementations) | `[MUST]` |
| `public class` (service) | `<summary>`, `<remarks>` (Domain, Dependencies) | `[MUST]` |
| `public method` | `<summary>`, `<param>`, `<returns>`, `<exception>` | `[MUST]` |
| `public property` | `<summary>` | `[SHOULD]` |
| `enum` values | `<summary>` | `[SHOULD]` |

### Bật trong `.csproj`
```xml
<PropertyGroup>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

### Generate API Docs
```bash
dotnet tool install -g docfx
docfx init -q && docfx build
```

> Ghi chú: `VAH.Backend/VAH.Backend.csproj` hiện chưa bao gồm `GenerateDocumentationFile`. Bật XML doc generation là tùy chọn; nếu muốn build-produced XML docs cho CI hoặc docfx, thêm snippet trên vào project file hoặc Directory.Build.props. Giữ `NoWarn` cho missing XML comments (`1591`) nếu không muốn block builds.

---

## §2 — JSDoc (React 19)

### Phạm Vi Bắt Buộc

| Thành phần | Tags Bắt Buộc | Mức độ |
|------------|---------------|--------|
| API service class | `@class`, `@extends`, `@description` | `[MUST]` |
| API method | `@param`, `@returns`, `@throws` | `[MUST]` |
| Custom hook | `@hook`, `@description`, `@returns`, `@dependency` | `[MUST]` |
| Component | `@component`, `@description`, `@param` (props) | `[SHOULD]` |

### Generate
```json
{ "scripts": { "docs": "jsdoc src/ -r -d docs/generated" } }
```

---

## §3 — Định Dạng ADR (Architecture Decision Record)

```markdown
# ADR-NNN: Title

**Status**: Proposed | Accepted | Deprecated | Superseded by ADR-XXX  
**Date**: YYYY-MM-DD  
**Deciders**: Names

## Context
What is the issue that we're seeing that is motivating this decision?

## Decision
What is the change that we're proposing/doing?

## Consequences
### Positive
### Negative
### Neutral

## Alternatives Considered
| Option | Pros | Cons | Rejected Because |
```

---

## §4 — Quy Ước File Markdown

| Quy tắc | Chuẩn |
|---------|-------|
| Headers | `#` → `##` → `###` (tối đa 3 cấp trong body) |
| Đánh số section | `§1`, `§2`, ... với subsections `1.1`, `1.2` |
| Code blocks | Triple backtick với language (`csharp`, `javascript`, `bash`) |
| Tables | Cho structured data, comparisons, rules |
| Diagrams | ASCII art (không phụ thuộc external image) |
| Ngôn ngữ | Kết hợp Tiếng Việt (giải thích) + English (code terms, headers) |
| Đặt tên file | `UPPER_SNAKE_CASE.md` |

---

> **Document End**
