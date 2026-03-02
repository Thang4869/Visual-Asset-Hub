# ADR-006: Strategy Pattern for Smart Collections

> **Status**: Accepted
> **Date**: 2026-02-27
> **Deciders**: Tech Lead

## Context

Smart Collections are dynamic, filter-based collections that display assets matching specific criteria (e.g., "all images", "recent uploads", "assets with tag X"). Each filter type has different query logic:

- **By Type**: filter by `ContentType` discriminator
- **By Tag**: filter by tag relationship
- **By Date Range**: filter by `CreatedAt` range
- **By Folder**: filter by `ParentFolderId`
- **Recent**: top N most recent assets

Adding new filter types should not require modifying existing filter implementations or the `SmartCollectionService`.

## Decision

Apply the **Strategy Pattern** via `ISmartCollectionFilter` interface:

```csharp
public interface ISmartCollectionFilter
{
    string FilterType { get; }        // Discriminator key
    bool CanHandle(string filterType); // Strategy selection
    IQueryable<Asset> Apply(IQueryable<Asset> query, SmartCollectionDefinition definition);
}
```

Concrete strategies:
- `TypeFilter` → filters by `ContentType`
- `TagFilter` → filters by tag name via `AssetTags` join
- `DateRangeFilter` → filters by `CreatedAt` between start/end
- `FolderFilter` → filters by `ParentFolderId`
- `RecentFilter` → orders by `CreatedAt DESC`, takes N

### Registration
All `ISmartCollectionFilter` implementations are registered individually and consumed via `IEnumerable<ISmartCollectionFilter>`:

```csharp
// In SmartCollectionService constructor
public SmartCollectionService(
    AppDbContext db,
    IEnumerable<ISmartCollectionFilter> filters) // All strategies injected
```

### Filter Selection
```csharp
var handler = _filters.FirstOrDefault(f => f.CanHandle(definition.FilterType))
    ?? throw new ArgumentException($"Unknown filter: {definition.FilterType}");
var filtered = handler.Apply(baseQuery, definition);
```

## Consequences

### Positive
- Open/Closed Principle: add new filter by implementing interface + registering in DI
- Each filter is independently testable
- Composable: multiple filters can be chained on the same `IQueryable<Asset>`
- No switch/if chains in `SmartCollectionService`

### Negative
- Filter discovery is runtime (`IEnumerable` iteration) — unknown filter type throws at runtime, not compile time
- All filters must work with `IQueryable<Asset>` (EF Core expression tree) — can't use in-memory logic

### Neutral
- Currently 5 concrete strategies — expected to grow as new smart collection types are requested
- Filters are registered as scoped services (same as other services)

## Compliance

- New smart collection filter types MUST implement `ISmartCollectionFilter`
- New filters MUST be registered in `ServiceCollectionExtensions.AddApplicationServices()`
- Filters MUST operate on `IQueryable<Asset>` (no materialization inside filter)
- Filter type strings must be documented in the Smart Collection module docs

---

> **Document End**
