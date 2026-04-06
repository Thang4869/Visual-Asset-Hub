# Search Module

> **Mục đích**: Tìm kiếm đa thực thể (assets + collections)
> **Last Updated**: 2026-04-06

---

## §1 — Tổng quan (Overview)

| Khía cạnh | Chi tiết |
|-----------|----------|
| **Domain** | Tìm kiếm đa thực thể (assets + collections) |
| **Service** | `ISearchService` → `SearchService` |
| **Controller** | `SearchController` (1 endpoint) |
| **DB** | EF Core LINQ queries (chưa có full-text index) |
| **Patterns** | Query Object (filter params), multi-entity aggregation |

## §2 — Giao diện Service (Service Interface)

```csharp
public interface ISearchService
{
    Task<SearchResult> SearchAsync(
        string userId,
        string? query,          // Text search term
        string? type,           // Filter by ContentType
        int? collectionId,      // Scope to collection
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default);
}
```

## §3 — API Endpoint

| Method | Route | Tham số Query |
|--------|-------|---------------|
| GET | `/api/v1/search` | `?query=&type=&collectionId=&page=&pageSize=` |

**Phản hồi (Response):**
```json
{
  "assets": { "items": [...], "totalCount": 42, "page": 1, "pageSize": 50 },
  "collections": [...]
}
```

## §4 — Chiến lược tìm kiếm (Search Strategy)

Triển khai hiện tại sử dụng LINQ `Contains` (SQL `LIKE '%term%'`):

1. **Tìm kiếm Asset**: Khớp với `FileName` và `Tags` (trường legacy phân tách bằng dấu phẩy)
2. **Tìm kiếm Collection**: Khớp với `Name` và `Description`
3. **Bộ lọc**: Tùy chọn `type` (ContentType) và phạm vi `collectionId`
4. **Phạm vi người dùng**: Chỉ trả về assets/collections thuộc sở hữu của người dùng hoặc system

### Cải tiến tương lai (Future Enhancement)
- PostgreSQL full-text search (`tsvector` + `tsquery`) cho production
- Tag-aware search qua `AssetTag` join (thay thế trường legacy `Tags`)
- Relevance scoring và ranking

---

> **Document End**
