# DOMAIN MODEL — Entity Relationships & Aggregates

> **Last Updated**: 2026-03-02

---

## §1 — Entity Relationship Diagram

```
┌──────────────────────────────────────────────────────────────────┐
│                    ApplicationUser (Identity)                     │
│  IdentityUser + DisplayName + CreatedAt                          │
│  PK: Id (string, GUID)                                          │
└──────┬────────────┬──────────────┬───────────────────────────────┘
       │ 1:N        │ 1:N          │ 1:N
       │            │              │
       ▼            ▼              ▼
┌──────────┐  ┌───────────┐  ┌───────────────────┐
│Collection│  │   Asset   │  │CollectionPermission│
│          │  │  (TPH)    │  │                    │
│ Id (PK)  │  │ Id (PK)   │  │ Id (PK)            │
│ Name     │  │ FileName  │  │ UserId (FK)         │
│ ParentId │──│ FilePath  │  │ CollectionId (FK)   │
│ UserId   │  │ ContentType│  │ Role (string)       │
│ Type     │  │ CollectionId│  │ GrantedBy           │
│ LayoutType│  │ UserId    │  │ GrantedAt            │
│ Color    │  │ GroupId   │  └───────────────────┘
│ Order    │  │ ParentFolderId│
└────┬─────┘  │ SortOrder │
     │ 1:N    │ Thumbnails│
     │        └─────┬─────┘
     │              │ M:N
     │              ▼
     │        ┌──────────┐     ┌──────────┐
     │        │ AssetTag  │────→│   Tag    │
     │        │ (Junction)│     │          │
     │        │ AssetId   │     │ Id (PK)  │
     │        │ TagId     │     │ Name     │
     │        └──────────┘     │ Normal.  │
     │                          │ Color    │
     │                          │ UserId   │
     └── self-ref (ParentId)    └──────────┘
```

## §2 — Aggregate Boundaries

### 2.1 Asset Aggregate (Root: `Asset`)

| Entity | Role | Invariants |
|--------|------|-----------|
| `Asset` (TPH base) | Aggregate root | UserId required for non-system assets; ContentType must match discriminator |
| `ImageAsset` | TPH subtype | `HasPhysicalFile = true`, `CanHaveThumbnails = true` |
| `LinkAsset` | TPH subtype | `HasPhysicalFile = false` — no file on disk |
| `ColorAsset` | TPH subtype | `HasPhysicalFile = false` — hex stored in Tags field |
| `ColorGroupAsset` | TPH subtype | `HasPhysicalFile = false` — groups ColorAssets via GroupId |
| `FolderAsset` | TPH subtype | `HasPhysicalFile = false`, `IsFolder = true` |
| `AssetTag` | Junction entity | Composite key (AssetId, TagId) |

**Domain Methods on Asset:**

```csharp
void UpdatePosition(double x, double y)   // Canvas layout coordinates
void ApplyUpdate(UpdateAssetDto dto)       // Partial update (null-safe)
void SetThumbnails(string? sm, md, lg)     // Server-generated thumbnails
void MoveToFolder(int? folderId)           // Re-parent within collection
void MoveToCollection(int collectionId)    // Cross-collection move
bool IsOwnedBy(string userId)             // Ownership check
AssetResponseDto ToDto()                   // Entity → DTO mapping
```

**TPH Discriminator Column**: `ContentType` (string in DB via `EnumMappings`)

```
ContentType value  →  EF Core .NET Type
─────────────────────────────────────────
"image"            →  ImageAsset
"link"             →  LinkAsset
"color"            →  ColorAsset
"color-group"      →  ColorGroupAsset
"folder"           →  FolderAsset
"file"             →  Asset (base, default)
```

### 2.2 Collection Aggregate (Root: `Collection`)

| Entity | Role | Invariants |
|--------|------|-----------|
| `Collection` | Aggregate root | Name required; hierarchical via ParentId (self-ref) |
| `CollectionPermission` | Value-like entity | Role ∈ {"owner", "editor", "viewer"} |

**Domain Methods on Collection:**

```csharp
bool IsOwnedBy(string userId)             // Direct ownership check
bool IsSystemCollection                    // UserId == null
bool IsAccessibleBy(string userId)         // System OR owned
void ApplyUpdate(UpdateCollectionDto dto)  // Partial update (null-safe)
```

**Role Authorization Matrix (`CollectionRoles`):**

| Role | CanWrite | CanManage |
|------|----------|-----------|
| `owner` | ✅ | ✅ |
| `editor` | ✅ | ❌ |
| `viewer` | ❌ | ❌ |

### 2.3 Tag Aggregate (Root: `Tag`)

| Entity | Role | Invariants |
|--------|------|-----------|
| `Tag` | Aggregate root | Name unique per user (via NormalizedName); auto-computed on SetName() |

**Domain Methods on Tag:**

```csharp
void SetName(string name)              // Trim + auto-set NormalizedName (lowercase)
void UpdateFrom(UpdateTagDto dto)      // Partial update, delegates to SetName
bool IsOwnedBy(string userId)          // Ownership check
```

### 2.4 Identity Aggregate (Root: `ApplicationUser`)

```csharp
public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

Extends ASP.NET Identity's `IdentityUser`. Managed entirely by Identity framework — no custom domain methods.

## §3 — Value Objects & Supporting Types

### 3.1 PagedResult\<T\>

Generic pagination wrapper used by all list endpoints:

```csharp
PagedResult<T> {
    List<T> Items
    int TotalCount, Page, PageSize
    bool HasNextPage, HasPreviousPage
    int TotalPages  // Computed: Ceiling(TotalCount / PageSize)
}
```

### 3.2 PaginationParams

Query parameter binding for paginated endpoints:

| Property | Default | Constraint |
|----------|---------|-----------|
| Page | 1 | ≥ 1 |
| PageSize | 50 | 1–100 |
| SortBy | null | Optional |
| SortOrder | "asc" | "asc" or "desc" |

### 3.3 Enum Types

| Enum | Values | DB Storage |
|------|--------|-----------|
| `AssetContentType` | Image, Link, Color, ColorGroup, Folder, File | String (lowercase via `EnumMappings`) |
| `CollectionType` | Default, Image, Link, Color | String (lowercase) |
| `LayoutType` | Grid, List, Canvas | String (lowercase) |

`EnumMappings` provides bidirectional conversion with `StringComparer.OrdinalIgnoreCase` for DB backward compatibility.

## §4 — Database Schema (EF Core 9 + PostgreSQL 17)

### 4.1 Table Mapping

| Entity | Table | Strategy | Notes |
|--------|-------|----------|-------|
| Asset + subtypes | `Assets` | TPH | Discriminator: `ContentType` (string) |
| Collection | `Collections` | Standard | Self-referential `ParentId` |
| Tag | `Tags` | Standard | Unique index on `(NormalizedName, UserId)` |
| AssetTag | `AssetTags` | Junction | Composite PK: `(AssetId, TagId)` |
| CollectionPermission | `CollectionPermissions` | Standard | FK to Collection |
| ApplicationUser | `AspNetUsers` | Identity | + `DisplayName`, `CreatedAt` columns |

### 4.2 Relationship Configuration

```
Asset.CollectionId    → Collection.Id      (Required, Cascade)
Asset.ParentFolderId  → Asset.Id           (Optional, self-ref, Restrict)
Collection.ParentId   → Collection.Id      (Optional, self-ref, Restrict)
AssetTag.AssetId      → Asset.Id           (Required, Cascade)
AssetTag.TagId        → Tag.Id             (Required, Cascade)
CollectionPermission.CollectionId → Collection.Id (Required, Cascade)
```

## §5 — Factory Pattern

`AssetFactory` provides 8 static creation methods + 1 duplication method:

```csharp
static Asset CreateImage(file, path, userId, collectionId)
static Asset CreateLink(name, url, userId, collectionId)
static Asset CreateColor(name, hex, userId, collectionId)
static Asset CreateColorGroup(name, userId, collectionId)
static Asset CreateFolder(name, userId, collectionId, parentFolderId?)
static Asset CreateFile(file, path, userId, collectionId)
static Asset FromUploadedFile(UploadedFileDto, userId, collectionId)
static Asset CreateForType(contentType, name, path, userId, collectionId)
static Asset Duplicate(source, userId)
```

Each factory method returns the correct TPH subtype (`ImageAsset`, `LinkAsset`, etc.) and sets all required fields including `CreatedAt = DateTime.UtcNow`.

---

> **Document End**
