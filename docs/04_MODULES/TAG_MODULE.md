# Tag Module

> **Mục đích**: Hệ thống tag cho assets (many-to-many)
> **Last Updated**: 2026-04-06

---

## §1 — Tổng quan (Overview)

| Khía cạnh | Chi tiết |
|-----------|----------|
| **Domain** | Hệ thống tag cho assets (many-to-many) |
| **Aggregate Root** | `Tag` |
| **Junction Table** | `AssetTag` (composite PK) |
| **Service** | `ITagService` → `TagService` |
| **Controller** | `TagsController` (10 endpoints) |
| **Patterns** | Normalized dedup, domain methods, batch operations |

## §2 — Domain Model

```csharp
public class Tag
{
    int Id                    // PK
    string Name               // Required, max 100
    string NormalizedName     // Auto-computed lowercase for dedup
    string? Color             // Optional badge color, max 20
    string? UserId            // Owner (null = system)
    DateTime CreatedAt

    // Navigation
    ICollection<AssetTag> AssetTags
}

public class AssetTag        // Junction table
{
    int AssetId              // Composite PK
    int TagId                // Composite PK
}
```

**Domain Methods:**

| Method | Mục đích |
|--------|----------|
| `SetName(name)` | Trim + tự động set `NormalizedName` (lowercase) |
| `UpdateFrom(dto)` | Cập nhật từng phần, ủy quyền cho `SetName` |
| `IsOwnedBy(userId)` | Kiểm tra quyền sở hữu |

## §3 — Giao diện Service (Service Interface)

```csharp
public interface ITagService
{
    // CRUD
    Task<List<Tag>> GetAllAsync(string userId, CancellationToken ct);
    Task<Tag> GetByIdAsync(int id, string userId, CancellationToken ct);
    Task<Tag> CreateAsync(CreateTagDto dto, string userId, CancellationToken ct);
    Task<Tag> UpdateAsync(int id, UpdateTagDto dto, string userId, CancellationToken ct);
    Task<bool> DeleteAsync(int id, string userId, CancellationToken ct);

    // Batch operations
    Task<List<Tag>> GetOrCreateTagsAsync(IEnumerable<string> tagNames, string userId, CancellationToken ct);

    // Quản lý quan hệ Asset-Tag
    Task SetAssetTagsAsync(int assetId, List<int> tagIds, string userId, CancellationToken ct);
    Task AddAssetTagsAsync(int assetId, List<int> tagIds, string userId, CancellationToken ct);
    Task RemoveAssetTagsAsync(int assetId, List<int> tagIds, string userId, CancellationToken ct);
    Task<List<Tag>> GetAssetTagsAsync(int assetId, string userId, CancellationToken ct);

    // Migration
    Task MigrateCommaSeparatedTagsAsync(string userId, CancellationToken ct);
}
```

## §4 — API Endpoints

| Method | Route | Mô tả |
|--------|-------|-------|
| GET | `/api/v1/tags` | Danh sách tag của người dùng |
| GET | `/api/v1/tags/{id}` | Lấy một tag |
| POST | `/api/v1/tags` | Tạo tag |
| PUT | `/api/v1/tags/{id}` | Cập nhật tag |
| DELETE | `/api/v1/tags/{id}` | Xóa tag |
| POST | `/api/v1/tags/get-or-create` | Batch: tìm hoặc tạo theo tên |
| PUT | `/api/v1/tags/assets/{assetId}` | Set (thay thế tất cả) tag của asset |
| POST | `/api/v1/tags/assets/{assetId}` | Thêm tag vào asset |
| DELETE | `/api/v1/tags/assets/{assetId}` | Xóa tag khỏi asset |
| GET | `/api/v1/tags/assets/{assetId}` | Lấy tất cả tag cho một asset |

## §5 — Chiến lược Deduplication (Deduplication Strategy)

Tag được dedup theo user qua `NormalizedName`:

```
Input: "  React JS  "
→ Name: "React JS"
→ NormalizedName: "react js"
```

- Unique index trên `(NormalizedName, UserId)` ngăn chặn tag trùng lặp theo user
- `GetOrCreateTagsAsync` kiểm tra NormalizedName trước khi tạo — idempotent

## §6 — Migration Legacy

Hệ thống ban đầu lưu tag dưới dạng chuỗi phân tách bằng dấu phẩy trong `Asset.Tags`:
```
"react, javascript, frontend"
```


`MigrateCommaSeparatedTagsAsync()` xử lý tất cả asset của một user:
1. Phân tích chuỗi `Asset.Tags` phân tách bằng dấu phẩy
2. Gọi `GetOrCreateTagsAsync` cho mỗi tên tag được phân tích
3. Tạo các bản ghi junction `AssetTag`
4. (Legacy) Đã comment lại dòng code cập nhật trường `asset.Tags` (chuẩn bị loại bỏ hoàn toàn trường này, chỉ còn dùng AssetTag junction)
5. Migration mới đồng bộ model, thêm các cột và index cần thiết cho hệ thống tag mới.

---

> **Document End**
