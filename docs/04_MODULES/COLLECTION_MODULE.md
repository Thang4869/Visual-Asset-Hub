# Collection Module

> **Mục đích**: Tổ chức tài sản theo cấu trúc phân cấp
> **Last Updated**: 2026-04-06

---

## §1 — Tổng quan (Overview)

| Khía cạnh | Chi tiết |
|-----------|----------|
| **Domain** | Tổ chức tài sản theo cấu trúc phân cấp |
| **Aggregate Root** | `Collection` |
| **Service** | `ICollectionService` → `CollectionService` |
| **Controller** | `CollectionsController` (7 endpoints) |
| **DB Table** | `Collections` |
| **Patterns** | Self-referential tree, domain methods, multi-tenancy qua UserId |

## §2 — Domain Model

```csharp
public class Collection
{
    int Id                    // PK
    string Name               // Required, max 255
    string Description        // max 2000
    int? ParentId             // Self-referential FK (tree)
    DateTime CreatedAt
    string Color              // max 20, default "#007bff"
    CollectionType Type       // Default, Image, Link, Color
    int Order                 // Display sort order
    LayoutType LayoutType     // Grid, List, Canvas
    string? UserId            // Owner (null = system)

    // Navigation
    ICollection<Asset> Assets
    Collection? Parent
    ICollection<Collection> Children
}
```

**Domain Methods:**

| Method | Mục đích |
|--------|----------|
| `IsOwnedBy(userId)` | Kiểm tra quyền sở hữu |
| `IsSystemCollection` | True khi `UserId == null` |
| `IsAccessibleBy(userId)` | System collection HOẶC sở hữu |
| `ApplyUpdate(dto)` | Cập nhật từng phần an toàn với null |

## §3 — Service Interface

```csharp
public interface ICollectionService
{
    Task<List<Collection>> GetAllAsync(string userId, CancellationToken ct);
    Task<Collection?> GetByIdAsync(int id, string userId, CancellationToken ct);
    Task<CollectionWithItemsResult> GetWithItemsAsync(int id, int? folderId, string userId, CancellationToken ct);
    Task<Collection> CreateAsync(CreateCollectionDto dto, string userId, CancellationToken ct);
    Task<Collection> UpdateAsync(int id, UpdateCollectionDto dto, string userId, CancellationToken ct);
    Task<bool> DeleteAsync(int id, string userId, CancellationToken ct);
}
```

## §4 — API Endpoints

| Method | Route | Mô tả |
|--------|-------|-------|
| GET | `/api/v1/collections` | Lấy danh sách collections của người dùng (sở hữu + system) |
| GET | `/api/v1/collections/{id}` | Lấy một collection (canonical resource endpoint) |
| GET | `/api/v1/collections/{id}/items` | Lấy collection với assets (tùy chọn `?folderId=`) |
| POST | `/api/v1/collections` | Tạo collection (201 Created → `GET {id}`) |
| PATCH | `/api/v1/collections/{id}` | Cập nhật từng phần |
| PUT | `/api/v1/collections/{id}` | Cập nhật toàn bộ (alias cho PATCH) |
| DELETE | `/api/v1/collections/{id}` | Xóa collection + cascade assets |

## §5 — Sequence Diagram — Tạo Collection

```
Client                CollectionsController    ICollectionService    AppDbContext
  │                         │                        │                   │
  │── POST /collections ───→│                        │                   │
  │   {name, type, color}   │                        │                   │
  │                         │── CreateAsync(dto) ───→│                   │
  │                         │                        │── new Collection()│
  │                         │                        │── db.Add() ──────→│
  │                         │                        │── SaveChanges ───→│
  │                         │                        │←── saved ─────────│
  │                         │←── Collection ─────────│                   │
  │←── 201 Created ─────────│                        │                   │
```

## §6 — Kiểm soát truy cập (Access Control)

Collections hỗ trợ hai mô hình truy cập:
1. **Quyền sở hữu**: Trường `UserId` — người dùng sở hữu collection
2. **Chia sẻ**: Qua `CollectionPermission` (xem PERMISSION_MODULE)
3. **System**: `UserId == null` — tất cả người dùng đã xác thực có thể truy cập

---

> **Document End**
