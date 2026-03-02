# SEARCH MODULE

> **Last Updated**: 2026-03-02
> **Status**: Active — Services/ layer

---

## §1 — Overview

| Aspect | Detail |
|--------|--------|
| **Domain** | Cross-entity search (assets + collections) |
| **Service** | `ISearchService` → `SearchService` |
| **Controller** | `SearchController` (1 endpoint) |
| **DB** | EF Core LINQ queries (no full-text index yet) |
| **Patterns** | Query Object (filter params), multi-entity aggregation |

## §2 — Service Interface

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

| Method | Route | Query Params |
|--------|-------|-------------|
| GET | `/api/v1/search` | `?query=&type=&collectionId=&page=&pageSize=` |

**Response:**
```json
{
  "assets": { "items": [...], "totalCount": 42, "page": 1, "pageSize": 50 },
  "collections": [...]
}
```

## §4 — Search Strategy

Current implementation uses LINQ `Contains` (SQL `LIKE '%term%'`):

1. **Asset search**: Matches against `FileName` and `Tags` (legacy comma-separated field)
2. **Collection search**: Matches against `Name` and `Description`
3. **Filters**: Optional `type` (ContentType) and `collectionId` scope
4. **User scope**: Only returns assets/collections owned by the user or system

### Future Enhancement
- PostgreSQL full-text search (`tsvector` + `tsquery`) for production
- Tag-aware search via `AssetTag` join (replacing legacy `Tags` field)
- Relevance scoring and ranking

---

> **Document End**
