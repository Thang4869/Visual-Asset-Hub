# Smart Collection Module

> **Mục đích**: Các collection động/ảo được tính toán tại thời điểm truy vấn
> **Last Updated**: 2026-04-06

---

## §1 — Tổng quan (Overview)

| Khía cạnh | Chi tiết |
|-----------|----------|
| **Domain** | Các collection động/ảo được tính toán tại thời điểm truy vấn |
| **Service** | `ISmartCollectionService` → `SmartCollectionService` |
| **Controller** | `SmartCollectionsController` (2 endpoints) |
| **Patterns** | Strategy Pattern (`ISmartCollectionFilter`), Open/Closed Principle |
| **ADR** | [ADR-006](../03_ARCHITECTURE/ADR/ADR-006_STRATEGY_SMART_COLLECTIONS.md) |

## §2 — Giao diện Service (Service Interface)

```csharp
public interface ISmartCollectionService
{
    Task<List<SmartCollectionDefinition>> GetDefinitionsAsync(string userId, CancellationToken ct);
    Task<PagedResult<AssetResponseDto>> GetItemsAsync(string smartCollectionId, PaginationParams pagination, string userId, CancellationToken ct);
}
```

## §3 — Giao diện Strategy (Strategy Interface)

```csharp
public interface ISmartCollectionFilter
{
    string FilterType { get; }
    bool CanHandle(string filterType);
    IQueryable<Asset> Apply(IQueryable<Asset> query, SmartCollectionDefinition definition);
}
```

## §4 — Các Strategy cụ thể (Concrete Strategies)

| Strategy | FilterType | Logic truy vấn |
|----------|----------|----------------|
| `TypeFilter` | `"by-type"` | `WHERE ContentType = @type` |
| `TagFilter` | `"by-tag"` | `JOIN AssetTags WHERE Tag.Name = @tag` |
| `DateRangeFilter` | `"by-date"` | `WHERE CreatedAt BETWEEN @start AND @end` |
| `FolderFilter` | `"by-folder"` | `WHERE ParentFolderId = @folderId` |
| `RecentFilter` | `"recent"` | `ORDER BY CreatedAt DESC TAKE @count` |

## §5 — API Endpoints

| Method | Route | Mô tả |
|--------|-------|-------|
| GET | `/api/v1/smart-collections` | Lấy tất cả định nghĩa smart collection |
| GET | `/api/v1/smart-collections/{id}/items` | Lấy các item theo phân trang khớp với tiêu chí |

## §6 — Cấu trúc định nghĩa (Definition Structure)

```json
{
  "id": "recent-images",
  "name": "Recent Images",
  "filterType": "by-type",
  "parameters": {
    "type": "image",
    "limit": 50
  }
}
```

Các định nghĩa được tạo ở phía server (không lưu trong DB) — chúng biểu diễn các cấu hình bộ lọc được định nghĩa trước.

## §7 — Thêm bộ lọc mới (Adding New Filters)

1. Tạo class implements `ISmartCollectionFilter`
2. Đăng ký trong DI (`services.AddScoped<ISmartCollectionFilter, YourFilter>()`)
3. Service tự động khám phá qua `IEnumerable<ISmartCollectionFilter>` injection
4. Không cần thay đổi `SmartCollectionService` hay controller (OCP)

---

> **Document End**
