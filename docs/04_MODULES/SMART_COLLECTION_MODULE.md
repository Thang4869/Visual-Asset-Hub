# SMART COLLECTION MODULE

> **Last Updated**: 2026-03-08
> **Status**: Active — Services/ layer

---

## §1 — Overview

| Aspect | Detail |
|--------|--------|
| **Domain** | Dynamic/virtual collections computed at query time |
| **Service** | `ISmartCollectionService` → `SmartCollectionService` |
| **Controller** | `SmartCollectionsController` (2 endpoints) |
| **Patterns** | Strategy Pattern (`ISmartCollectionFilter`), Open/Closed Principle |
| **ADR** | [ADR-006](../03_ARCHITECTURE/ADR/ADR-006_STRATEGY_SMART_COLLECTIONS.md) |

## §2 — Service Interface

```csharp
public interface ISmartCollectionService
{
    Task<List<SmartCollectionDefinition>> GetDefinitionsAsync(string userId, CancellationToken ct);
    Task<PagedResult<AssetResponseDto>> GetItemsAsync(string smartCollectionId, PaginationParams pagination, string userId, CancellationToken ct);
}
```

## §3 — Strategy Interface

```csharp
public interface ISmartCollectionFilter
{
    string FilterType { get; }
    bool CanHandle(string filterType);
    IQueryable<Asset> Apply(IQueryable<Asset> query, SmartCollectionDefinition definition);
}
```

## §4 — Concrete Strategies

| Strategy | FilterType | Query Logic |
|----------|----------|-------------|
| `TypeFilter` | `"by-type"` | `WHERE ContentType = @type` |
| `TagFilter` | `"by-tag"` | `JOIN AssetTags WHERE Tag.Name = @tag` |
| `DateRangeFilter` | `"by-date"` | `WHERE CreatedAt BETWEEN @start AND @end` |
| `FolderFilter` | `"by-folder"` | `WHERE ParentFolderId = @folderId` |
| `RecentFilter` | `"recent"` | `ORDER BY CreatedAt DESC TAKE @count` |

## §5 — API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/v1/smart-collections` | Get all smart collection definitions |
| GET | `/api/v1/smart-collections/{id}/items` | Get paginated items matching criteria |

## §6 — Definition Structure

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

Definitions are generated server-side (not stored in DB) — they represent pre-defined filter configurations.

## §7 — Adding New Filters

1. Create class implementing `ISmartCollectionFilter`
2. Register in DI (`services.AddScoped<ISmartCollectionFilter, YourFilter>()`)
3. Service auto-discovers via `IEnumerable<ISmartCollectionFilter>` injection
4. No changes needed to `SmartCollectionService` or controller (OCP)

---

> **Document End**
