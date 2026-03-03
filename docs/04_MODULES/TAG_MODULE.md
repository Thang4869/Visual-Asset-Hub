# TAG MODULE

> **Last Updated**: 2026-03-02
> **Status**: Active — Services/ layer

---

## §1 — Overview

| Aspect | Detail |
|--------|--------|
| **Domain** | Tagging system for assets (many-to-many) |
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

| Method | Purpose |
|--------|---------|
| `SetName(name)` | Trim + auto-set `NormalizedName` (lowercase) |
| `UpdateFrom(dto)` | Partial update, delegates to `SetName` |
| `IsOwnedBy(userId)` | Ownership check |

## §3 — Service Interface

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

    // Asset-Tag relationship management
    Task SetAssetTagsAsync(int assetId, List<int> tagIds, string userId, CancellationToken ct);
    Task AddAssetTagsAsync(int assetId, List<int> tagIds, string userId, CancellationToken ct);
    Task RemoveAssetTagsAsync(int assetId, List<int> tagIds, string userId, CancellationToken ct);
    Task<List<Tag>> GetAssetTagsAsync(int assetId, string userId, CancellationToken ct);

    // Migration
    Task MigrateCommaSeparatedTagsAsync(string userId, CancellationToken ct);
}
```

## §4 — API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/v1/tags` | List user's tags |
| GET | `/api/v1/tags/{id}` | Get single tag |
| POST | `/api/v1/tags` | Create tag |
| PUT | `/api/v1/tags/{id}` | Update tag |
| DELETE | `/api/v1/tags/{id}` | Delete tag |
| POST | `/api/v1/tags/get-or-create` | Batch: find or create by names |
| PUT | `/api/v1/tags/assets/{assetId}` | Set (replace all) asset tags |
| POST | `/api/v1/tags/assets/{assetId}` | Add tags to asset |
| DELETE | `/api/v1/tags/assets/{assetId}` | Remove tags from asset |
| GET | `/api/v1/tags/assets/{assetId}` | Get all tags for an asset |

## §5 — Deduplication Strategy

Tags are deduplicated per user via `NormalizedName`:

```
Input: "  React JS  "
→ Name: "React JS"
→ NormalizedName: "react js"
```

- Unique index on `(NormalizedName, UserId)` prevents duplicate tags per user
- `GetOrCreateTagsAsync` checks NormalizedName before creating — idempotent

## §6 — Legacy Migration

The system originally stored tags as comma-separated strings in `Asset.Tags`:
```
"react, javascript, frontend"
```

`MigrateCommaSeparatedTagsAsync()` processes all assets for a user:
1. Parses comma-separated `Asset.Tags` string
2. Calls `GetOrCreateTagsAsync` for each parsed tag name
3. Creates `AssetTag` junction records
4. Clears the legacy `Asset.Tags` field

---

> **Document End**
