# DATABASE CONVENTIONS — EF Core & PostgreSQL

> **Last Updated**: 2026-03-02  
> **ORM**: Entity Framework Core 9  
> **Providers**: PostgreSQL 17 (production) / SQLite (development)

---

## §1 — Dual Provider Architecture

```csharp
// Configured in ServiceCollectionExtensions.AddDatabase()
var dbProvider = configuration.GetValue<string>("DatabaseProvider") ?? "SQLite";
// PostgreSQL for Docker/production, SQLite for local development
```

`DatabaseProviderInfo` record injected as Singleton for runtime provider detection.

## §2 — Entity Configuration

### Convention: Data Annotations + Fluent API
- `[Key]`, `[Required]`, `[MaxLength]` on entities (current state)
- **Target**: Migrate to Fluent API via `IEntityTypeConfiguration<T>` (tech debt item)

### TPH Discriminator
```csharp
// Asset table — ContentType column as discriminator
// Configured automatically by EF Core from subclass hierarchy:
// Asset (base) → ImageAsset, LinkAsset, ColorAsset, ColorGroupAsset, FolderAsset
// Enum stored as lowercase string via EnumMappings
```

## §3 — Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Table | Plural PascalCase (EF default) | `Assets`, `Collections`, `Tags` |
| Column | PascalCase (EF default) | `FileName`, `CreatedAt`, `ParentFolderId` |
| FK column | `{NavigationProperty}Id` | `CollectionId`, `UserId`, `GroupId` |
| Junction table | `{Entity1}{Entity2}` | `AssetTags` (auto M:N) |
| Index | `IX_{Table}_{Column}` | `IX_Assets_UserId` |
| Migration | `{Timestamp}_{Description}` | `20260225203615_InitialCreate` |

## §4 — Current Schema (6 Tables)

```
┌─────────────────┐     ┌─────────────────┐    ┌──────────────────┐
│ AspNetUsers     │     │ Collections     │    │ Tags             │
│ (Identity)      │←───┐│ Id, Name, Type  │    │ Id, Name, Color  │
│ Id, Email, ...  │    ││ ParentId (self) │    │ NormalizedName   │
└─────────────────┘    ││ UserId (FK)     │    │ UserId (FK)      │
         │             ││ LayoutType      │    └────────┬─────────┘
         │             │└────────┬────────┘             │
         │             │         │                      │
         ▼             │         ▼                      ▼
┌──────────────────┐   │ ┌─────────────────┐    ┌──────────────────┐
│ Assets (TPH)     │───┘ │ CollectionPerm. │    │ AssetTags (M:N)  │
│ Id, FileName     │     │ Id, CollectionId│    │ AssetId (FK)     │
│ FilePath, Tags   │     │ UserId, Role    │    │ TagId (FK)       │
│ ContentType      │     │ GrantedByUserId │    │                  │
│ CollectionId FK  │     └─────────────────┘    └──────────────────┘
│ UserId FK        │
│ ParentFolderId   │ (self-ref FK)
│ GroupId          │
│ ThumbnailSm/Md/Lg│
└──────────────────┘
```

## §5 — Migration Rules

| Rule | Detail |
|------|--------|
| Auto-migrate on startup | `db.Database.Migrate()` in `Program.cs` |
| Never edit generated migrations | Regenerate if needed |
| Migration naming | Descriptive: `AddTagSystem`, `AddThumbnailColumns` |
| Data fixups | Raw SQL in `Program.cs` after migrate (ContentType discriminator fix) |
| **Production**: Disable auto-migrate | Use `dotnet ef database update` manually |

## §6 — Query Patterns

```csharp
// ✅ Always filter by UserId (data isolation)
var assets = await _context.Assets
    .Where(a => a.UserId == userId)
    .OrderBy(a => a.SortOrder)
    .ToListAsync(ct);

// ✅ Use Include() explicitly (no lazy loading)
var collection = await _context.Collections
    .Include(c => c.Assets)
    .Include(c => c.Children)
    .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct);

// ✅ Map to DTO before returning
return assets.Select(a => a.ToDto()).ToList();

// ❌ Never return IQueryable from service
// ❌ Never use Find() without ownership check
```

## §7 — Current Migrations (5)

| Migration | Date | Description |
|-----------|------|-------------|
| `InitialCreate` | 2026-02-25 | Base schema: Assets, Collections, Identity |
| `AddThumbnailColumns` | 2026-02-26 | ThumbnailSm/Md/Lg on Assets |
| `AddTagSystem` | 2026-02-27 | Tags + AssetTags M:N |
| `AddCollectionPermissions` | 2026-02-27 | CollectionPermission table |
| `SyncModelChanges` | 2026-02-27 | Model cleanup alignment |

---

> **Document End**
