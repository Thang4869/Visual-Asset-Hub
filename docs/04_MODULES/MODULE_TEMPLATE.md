# MODULE TEMPLATE

> **Hướng dẫn**: Copy file này khi tạo documentation cho module mới.  
> Thay thế tất cả `{placeholder}` bằng nội dung thực tế.  
> Xóa các comment hướng dẫn (dòng bắt đầu bằng `>`) sau khi hoàn thành.

---

# {Module Name} Module

> **Domain**: {Core | Supporting | Generic}  
> **Status**: {Active | Planned | Deprecated}  
> **Owner**: {Team/Person}  
> **Last Updated**: {YYYY-MM-DD}

---

## §1 — Mục đích (Purpose)

### 1.1 Problem Statement
> {Module này giải quyết bài toán gì? Tại sao cần tách thành module riêng?}

### 1.2 Scope
> {Module này chịu trách nhiệm gì? Boundary rõ ràng.}

### 1.3 Out of Scope
> {Những gì KHÔNG thuộc module này — tránh Feature Envy.}

---

## §2 — Kiến trúc tổng quan (Architecture Overview)

### 2.1 Layer Mapping

```
Presentation       → {Controllers, Hubs liên quan}
Application        → {Services, CQRS Handlers}
Domain             → {Entities, Value Objects, Enums}
Infrastructure     → {Repository, External integrations}
```

### 2.2 Component Diagram

```
┌──────────────┐     ┌──────────────────┐     ┌────────────────┐
│  Controller   │────→│  I{Module}Service │────→│  AppDbContext   │
│  {name}       │     │  ({n} methods)   │     │  {Entity}       │
└──────────────┘     └──────────────────┘     └────────────────┘
                           │
                     ┌─────┴─────┐
                     │ {Dependency} │
                     └───────────┘
```

---

## §3 — Interfaces chính (Key Interfaces)

### 3.1 `I{Module}Service`

> {Mô tả interface — nó giải quyết vấn đề gì}

```csharp
public interface I{Module}Service
{
    // Liệt kê methods với XML doc summary
    Task<{ResponseDto}> Get{Entity}Async({params}, CancellationToken ct = default);
    Task<{ResponseDto}> Create{Entity}Async({dto}, string userId, CancellationToken ct = default);
    // ...
}
```

**Tại sao cần interface này?**
> {Giải thích — testability, DIP, swappable implementations, etc.}

### 3.2 Các interface phụ trợ

> {Liệt kê các interface khác thuộc module, nếu có}

---

## §4 — Domain Entities

### 4.1 `{Entity}`

```csharp
// Entity structure overview
public class {Entity}
{
    // Identity
    public {IdType} Id { get; private set; }
    
    // Properties
    // ...
    
    // Navigation
    // ...
    
    // Domain methods
    // ...
}
```

**Invariants:**
> {Các business rules mà entity phải đảm bảo}

**Relationships:**
> {Entity này liên kết với entity nào? 1:N, M:N, etc.}

---

## §5 — Design Patterns Used

| Pattern | Áp dụng | Lý do |
|---------|---------|-------|
| {Pattern} | {Component} | {Vấn đề giải quyết} |

---

## §6 — Luồng xử lý (Sequence Logic)

### 6.1 Use Case: {Primary Use Case}

```
Actor           Controller          Service            DbContext          External
  │                │                   │                   │                 │
  │── {action} ──→│                   │                   │                 │
  │                │── {method}() ──→│                   │                 │
  │                │                   │── Query ────────→│                 │
  │                │                   │←── Entity ───────│                 │
  │                │                   │── {ext call} ──→│                 │──→ ...
  │                │                   │←── Result ──────│                 │
  │                │←── DTO ──────────│                   │                 │
  │←── HTTP 200 ──│                   │                   │                 │
```

### 6.2 Use Case: {Secondary Use Case}

> {Repeat pattern}

---

## §7 — API Endpoints

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `GET` | `/api/v1/{module}` | {description} | `[Authorize]` |
| `POST` | `/api/v1/{module}` | {description} | `[Authorize]` |
| `PUT` | `/api/v1/{module}/{id}` | {description} | `[Authorize]` |
| `DELETE` | `/api/v1/{module}/{id}` | {description} | `[Authorize]` |

---

## §8 — DTOs

### 8.1 Request DTOs

```csharp
public record Create{Entity}Dto({params});
public record Update{Entity}Dto({params});
```

### 8.2 Response DTOs

```csharp
public record {Entity}ResponseDto
{
    // properties
}
```

---

## §9 — Dependencies

### 9.1 This Module Depends On:
| Module/Service | Reason |
|---------------|--------|
| {dependency} | {why} |

### 9.2 Depended On By:
| Module/Service | Reason |
|---------------|--------|
| {consumer} | {why} |

---

## §10 — Testing Strategy

| Test Type | Coverage Target | Tools |
|-----------|----------------|-------|
| Unit Tests | Service methods | xUnit, Moq |
| Integration Tests | API endpoints | WebApplicationFactory |
| E2E Tests | User workflows | Playwright |

---

## §11 — Known Issues & Technical Debt

| # | Issue | Severity | Planned Fix |
|---|-------|----------|-------------|
| 1 | {issue description} | {High/Medium/Low} | {Sprint/Phase} |

---

> **Document End**  
> Related: [ARCHITECTURE_CONVENTIONS.md](../01_DESIGN_PHILOSOPHY/ARCHITECTURE_CONVENTIONS.md)
