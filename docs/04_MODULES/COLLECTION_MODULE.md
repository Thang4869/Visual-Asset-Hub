# COLLECTION MODULE

> **Last Updated**: 2026-03-02
> **Status**: Active — Services/ layer (not yet migrated to Features/)

---

## §1 — Overview

| Aspect | Detail |
|--------|--------|
| **Domain** | Hierarchical organization of assets |
| **Aggregate Root** | `Collection` |
| **Service** | `ICollectionService` → `CollectionService` |
| **Controller** | `CollectionsController` (6 endpoints) |
| **DB Table** | `Collections` |
| **Patterns** | Self-referential tree, domain methods, multi-tenancy via UserId |

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

| Method | Purpose |
|--------|---------|
| `IsOwnedBy(userId)` | Ownership check |
| `IsSystemCollection` | True when `UserId == null` |
| `IsAccessibleBy(userId)` | System collection OR owned |
| `ApplyUpdate(dto)` | Null-safe partial update |

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

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/v1/collections` | List user's collections (owned + system) |
| GET | `/api/v1/collections/{id}` | Get single collection |
| GET | `/api/v1/collections/{id}/items` | Get collection with assets (optional `?folderId=`) |
| POST | `/api/v1/collections` | Create collection |
| PUT | `/api/v1/collections/{id}` | Full/partial update |
| DELETE | `/api/v1/collections/{id}` | Delete collection + cascade assets |

## §5 — Sequence Diagram — Create Collection

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

## §6 — Access Control

Collections support two access models:
1. **Ownership**: `UserId` field — user owns the collection
2. **Sharing**: Via `CollectionPermission` (see PERMISSION_MODULE)
3. **System**: `UserId == null` — accessible by all authenticated users

---

> **Document End**
