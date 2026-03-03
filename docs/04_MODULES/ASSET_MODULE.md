# Asset Module

> **Domain**: Core (Asset Management)  
> **Status**: Active  
> **Owner**: Backend Team  
> **Last Updated**: 2026-03-02

---

## §1 — Mục đích (Purpose)

### 1.1 Problem Statement

VAH là nền tảng quản lý tài sản kỹ thuật số (Digital Asset Management) cho cá nhân và nhóm nhỏ. **Asset** là entity trung tâm của hệ thống — đại diện cho mọi loại tài nguyên số mà người dùng quản lý: ảnh, link, màu sắc, nhóm màu, và thư mục.

Bài toán cốt lõi: Quản lý **đa dạng loại asset** (5+ content types) với chung một pipeline CRUD nhưng **behavior khác nhau** (ảnh cần thumbnail, link không có file vật lý, folder chứa asset khác). Nếu dùng `if/switch` trên type → vi phạm OCP, khó mở rộng.

### 1.2 Scope

| Thuộc module này | Mô tả |
|-----------------|-------|
| Asset CRUD | Tạo, đọc, cập nhật, xóa asset |
| File Upload | Upload file → tạo ImageAsset + thumbnail |
| Asset Types | 6 content types qua TPH inheritance |
| Canvas Layout | Position (x, y), reorder, sort order |
| Folder Hierarchy | Asset chứa trong folder (self-referencing) |
| Asset Duplication | Clone asset cùng metadata |
| Domain-specific Creation | CreateFolder, CreateColor, CreateLink, CreateColorGroup |

### 1.3 Out of Scope

| Không thuộc module này | Thuộc module |
|-----------------------|-------------|
| Bulk operations (batch delete, move, tag) | **Bulk Module** (`IBulkAssetService`) |
| File storage I/O (upload/delete physical files) | **Storage Module** (`IStorageService`) |
| Thumbnail generation (image processing) | **Storage Module** (`IThumbnailService`) |
| Tag management (create/delete tags) | **Tag Module** (`ITagService`) |
| Collection management | **Collection Module** (`ICollectionService`) |
| Permission checks | **Permission Module** (`IPermissionService`) |
| Real-time notifications | **Realtime Module** (`INotificationService`) |
| Search & filtering | **Search Module** (`ISearchService`) |

---

## §2 — Kiến trúc tổng quan (Architecture Overview)

### 2.1 Layer Mapping

```
┌───────────────────────────────────────────────────────────────────┐
│ PRESENTATION LAYER                                                │
│                                                                   │
│  AssetLayoutController           Features/Assets/Commands/        │
│  (position, reorder)             AssetsCommandController          │
│                                  (create, upload, update, delete) │
│                                  Features/Assets/Queries/         │
│                                  AssetsQueryController            │
│                                                                   │
│  CQRS Records:                                                    │
│  ├── Commands/ (5 commands)                                       │
│  └── Queries/  (3 queries)                                        │
├───────────────────────────────────────────────────────────────────┤
│ APPLICATION LAYER                                                 │
│                                                                   │
│  IAssetService (14 methods) ← AssetService (~280 LOC)             │
│  IBulkAssetService (4 methods) ← BulkAssetService                 │
│  AssetCleanupHelper (file + thumbnail cleanup)                    │
│                                                                   │
│  CQRS Handlers:                                                   │
│  ├── AssetCommandHandlers (5 handlers → delegate to IAssetService)│
│  └── AssetQueryHandlers   (3 handlers → delegate to IAssetService)│
├───────────────────────────────────────────────────────────────────┤
│ DOMAIN LAYER                                                      │
│                                                                   │
│  Asset (base class) ← TPH discriminator: ContentType              │
│  ├── ImageAsset    (HasPhysicalFile=true, CanHaveThumbnails=true) │
│  ├── LinkAsset     (HasPhysicalFile=false)                        │
│  ├── ColorAsset    (HasPhysicalFile=false)                        │
│  ├── ColorGroupAsset (HasPhysicalFile=false)                      │
│  └── FolderAsset   (HasPhysicalFile=false)                        │
│                                                                   │
│  AssetFactory (static factory — 7 creation methods + Duplicate)   │
│  AssetContentType (enum — 6 values + bidirectional DB mapping)    │
│  AssetResponseDto, CreateAssetDto, UpdateAssetDto, ...            │
├───────────────────────────────────────────────────────────────────┤
│ INFRASTRUCTURE LAYER                                              │
│                                                                   │
│  AppDbContext.Assets (DbSet<Asset> — TPH configuration)           │
│  IStorageService → LocalStorageService (file I/O)                 │
│  IThumbnailService → ThumbnailService (ImageSharp)                │
└───────────────────────────────────────────────────────────────────┘
```

### 2.2 Component Dependency Diagram

```
┌──────────────────────┐     ┌────────────────────────┐
│ AssetLayoutController│     │ AssetsCommandController│
│ (2 endpoints)        │     │ (5 endpoints)          │
└────────┬─────────────┘     └────────┬───────────────┘
         │                            │
         │     ┌──────────────┐       │     ┌──────────────────┐
         └────→│ IAssetService│←──────┘     │ AssetsQueryCtrl  │
               │ (14 methods) │←────────────│ (3 endpoints)    │
               └──────┬───────┘             └──────────────────┘
                      │
          ┌───────────┼───────────┬──────────────────┐
          ▼           ▼           ▼                  ▼
   ┌────────────┐ ┌──────────┐ ┌──────────────┐ ┌─────────────────┐
   │AppDbContext│ │IStorage  │ │IThumbnail    │ │INotification    │
   │(Assets)    │ │Service   │ │Service       │ │Service          │
   └────────────┘ └──────────┘ └──────────────┘ └─────────────────┘
                      │              │
                      ▼              ▼
               ┌────────────┐ ┌───────────────┐
               │LocalStorage│ │ThumbnailSvc   │
               │Service     │ │(ImageSharp)   │
               └────────────┘ └───────────────┘
```

---

## §3 — Interfaces chính (Key Interfaces)

### 3.1 `IAssetService` — Core Asset Operations

```csharp
/// <summary>
/// Defines the contract for managing Asset lifecycle operations.
/// </summary>
/// <remarks>
/// <para><b>Domain:</b> Core (Asset Management)</para>
/// <para><b>Implementation:</b> AssetService (~280 LOC)</para>
/// <para><b>Consumers:</b> AssetLayoutController, AssetsCommandController, 
///   AssetsQueryController, CQRS Handlers</para>
/// <para><b>Dependencies (impl):</b> AppDbContext, IStorageService, 
///   IThumbnailService, INotificationService, ILogger</para>
/// </remarks>
public interface IAssetService
{
    // ── Queries ──
    Task<PagedResult<AssetResponseDto>> GetAssetsAsync(
        PaginationParams pagination, string userId, CancellationToken ct = default);
    Task<AssetResponseDto> GetByIdAsync(int id, string userId, CancellationToken ct = default);
    Task<List<AssetResponseDto>> GetAssetsByGroupAsync(
        int groupId, string userId, CancellationToken ct = default);
    
    // ── Commands — Generic ──
    Task<AssetResponseDto> CreateAssetAsync(
        CreateAssetDto dto, string userId, CancellationToken ct = default);
    Task<List<AssetResponseDto>> UploadFilesAsync(
        IReadOnlyCollection<UploadedFileDto> files, int collectionId, 
        int? folderId, string userId, CancellationToken ct = default);
    Task<AssetResponseDto> UpdateAssetAsync(
        int id, UpdateAssetDto dto, string userId, CancellationToken ct = default);
    Task<bool> DeleteAssetAsync(int id, string userId, CancellationToken ct = default);
    Task<AssetResponseDto> DuplicateAssetAsync(
        int id, int? targetFolderId, string userId, CancellationToken ct = default);
    
    // ── Commands — Domain-specific Creation ──
    Task<AssetResponseDto> CreateFolderAsync(
        CreateFolderDto dto, string userId, CancellationToken ct = default);
    Task<AssetResponseDto> CreateColorAsync(
        CreateColorDto dto, string userId, CancellationToken ct = default);
    Task<AssetResponseDto> CreateColorGroupAsync(
        CreateColorGroupDto dto, string userId, CancellationToken ct = default);
    Task<AssetResponseDto> CreateLinkAsync(
        CreateLinkDto dto, string userId, CancellationToken ct = default);
    
    // ── Commands — Layout ──
    Task<AssetResponseDto> UpdatePositionAsync(
        int id, double positionX, double positionY, string userId, CancellationToken ct = default);
    Task ReorderAssetsAsync(List<int> assetIds, string userId, CancellationToken ct = default);
}
```

**Tại sao cần interface `IAssetService`?**

| Lý do | Giải thích |
|-------|-----------|
| **DIP (Dependency Inversion)** | Controller depend on abstraction, không biết AssetService dùng EF Core hay Dapper |
| **Testability** | Mock `IAssetService` khi unit test Controller — không cần DB thật |
| **Swappable** | Có thể tạo `CachedAssetService` (decorator) wrap `AssetService` mà không sửa Controller |
| **Contract clarity** | Interface = API contract rõ ràng cho consumers |

### 3.2 `IBulkAssetService` — Batch Operations (ISP)

```csharp
/// <summary>
/// Handles bulk operations on assets (delete, move, tag).
/// ISP: Separated from IAssetService so consumers needing only bulk ops
/// don't depend on the full single-asset interface.
/// </summary>
public interface IBulkAssetService
{
    Task<int> BulkDeleteAsync(List<int> assetIds, string userId, CancellationToken ct = default);
    Task<int> BulkMoveAsync(BulkMoveDto dto, string userId, CancellationToken ct = default);
    Task<int> BulkMoveGroupAsync(BulkMoveGroupDto dto, string userId, CancellationToken ct = default);
    Task<int> BulkTagAsync(BulkTagDto dto, string userId, CancellationToken ct = default);
}
```

**Tại sao tách `IBulkAssetService` ra khỏi `IAssetService`?**

- **ISP (Interface Segregation)**: `AssetLayoutController` chỉ cần position/reorder — không bao giờ dùng bulk operations
- **SRP**: Bulk operations có logic phức tạp riêng (transaction, batch validation, rollback) 
- **Performance**: Bulk operations có optimization strategies khác (batch SQL, streaming)

### 3.3 Infrastructure Interfaces Used

| Interface | Methods | Mục đích | Implementation |
|-----------|---------|----------|----------------|
| `IStorageService` | 4 | File upload/delete/exists/URL | `LocalStorageService` (→ S3 future) |
| `IThumbnailService` | 1 | Generate 3 thumbnail sizes (sm/md/lg WebP) | `ThumbnailService` (ImageSharp) |
| `INotificationService` | ~2 | Push real-time updates via SignalR | `NotificationService` |

---

## §4 — Domain Entities

### 4.1 `Asset` (Base Entity — TPH)

```csharp
public class Asset
{
    // ── Identity ──
    public int Id { get; set; }
    
    // ── Core Properties ──
    public string FileName { get; set; }    // Display name
    public string FilePath { get; set; }    // Storage path / URL / hex code
    public string Tags { get; set; }        // Legacy string-based tags
    public DateTime CreatedAt { get; set; }
    public AssetContentType ContentType { get; set; }   // TPH discriminator
    
    // ── Position & Layout ──
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public int SortOrder { get; set; }
    
    // ── Hierarchy ──
    public int CollectionId { get; set; }       // Parent collection (FK)
    public int? GroupId { get; set; }           // Color group (FK, nullable)
    public int? ParentFolderId { get; set; }    // Folder hierarchy (self-ref FK)
    public bool IsFolder { get; set; }
    
    // ── Ownership ──
    public string? UserId { get; set; }         // FK → ApplicationUser
    
    // ── Thumbnails ──
    public string? ThumbnailSm { get; set; }    // 150px WebP
    public string? ThumbnailMd { get; set; }    // 400px WebP
    public string? ThumbnailLg { get; set; }    // 800px WebP
    
    // ── Navigation ──
    public ICollection<AssetTag> AssetTags { get; set; }
    public Collection? Collection { get; set; }
    public Asset? ParentFolder { get; set; }
    
    // ── Virtual Behavior Properties (TPH Polymorphism) ──
    public virtual bool HasPhysicalFile => true;
    public virtual bool CanHaveThumbnails => false;
    public virtual bool RequiresFileCleanup => HasPhysicalFile && FilePath.StartsWith("/uploads/");
    
    // ── Domain Methods ──
    public void UpdatePosition(double x, double y);
    public void ApplyUpdate(UpdateAssetDto dto);
    public void SetThumbnails(string? sm, string? md, string? lg);
    public void MoveToFolder(int? folderId);
    public void MoveToCollection(int collectionId);
    public bool IsOwnedBy(string userId);
    public AssetResponseDto ToDto();
}
```

**Invariants:**
| # | Business Rule | Enforcement |
|---|--------------|-------------|
| INV-01 | `FileName` cannot be empty | `[Required]` + `ApplyUpdate()` trims |
| INV-02 | Asset belongs to exactly 1 Collection | `CollectionId` non-nullable FK |
| INV-03 | Only owner can modify asset | `IsOwnedBy(userId)` check in Service |
| INV-04 | ImageAsset has physical file | `HasPhysicalFile` override = `true` |
| INV-05 | Non-image types have no physical file | `HasPhysicalFile` override = `false` |

**Relationships:**

```
Asset (N) ─────→ (1) Collection        via CollectionId
Asset (N) ─────→ (1) ApplicationUser   via UserId
Asset (N) ─────→ (1) Asset (Folder)    via ParentFolderId (self-ref)
Asset (N) ←──M──→ (N) Tag              via AssetTag (M:N join table)
Asset (1) ←─────→ (N) Asset (Group)    via GroupId (ColorGroup contains Colors)
```

### 4.2 TPH Subtypes

| Subtype | `ContentType` | `HasPhysicalFile` | `CanHaveThumbnails` | `FilePath` chứa |
|---------|--------------|-------------------|--------------------|-|
| `Asset` (base) | `File` | `true` | `false` | Upload path |
| `ImageAsset` | `Image` | `true` | `true` | Upload path |
| `LinkAsset` | `Link` | `false` | `false` | URL |
| `ColorAsset` | `Color` | `false` | `false` | Hex code (`#FF5733`) |
| `ColorGroupAsset` | `ColorGroup` | `false` | `false` | Empty |
| `FolderAsset` | `Folder` | `false` | `false` | Empty |

### 4.3 `AssetFactory` — Factory Pattern

```csharp
public static class AssetFactory
{
    // 6 factory methods — mỗi method tạo đúng TPH subtype
    public static ImageAsset CreateImage(fileName, filePath, collectionId, userId, parentFolderId?);
    public static Asset CreateFile(fileName, filePath, collectionId, userId, parentFolderId?);
    public static FolderAsset CreateFolder(name, collectionId, userId, parentFolderId?);
    public static ColorAsset CreateColor(colorCode, collectionId, userId, colorName?, groupId?, ...);
    public static ColorGroupAsset CreateColorGroup(groupName, collectionId, userId, ...);
    public static LinkAsset CreateLink(name, url, collectionId, userId, parentFolderId?);
    
    // Duplicate — clone bất kỳ asset type nào
    public static Asset Duplicate(Asset source, string userId, int? targetFolderId = null);
    
    // From DTO — generic file creation
    public static Asset FromDto(CreateAssetDto dto, string userId);
}
```

**Tại sao dùng Factory thay vì `new Asset()`?**

| Lý do | Giải thích |
|-------|-----------|
| **Correct TPH subtype** | `new Asset()` tạo base type → EF Core set sai discriminator. Factory đảm bảo `new ImageAsset()` cho image |
| **Encapsulated creation logic** | Default values, `CreatedAt = DateTime.UtcNow`, `ContentType` assignment — tất cả tập trung 1 chỗ |
| **Consistent initialization** | Mọi caller dùng chung factory → không ai quên set `UserId` hoặc `CreatedAt` |
| **Duplicate correctness** | `Duplicate()` dùng switch expression để tạo đúng subtype từ `ContentType` enum |

### 4.4 `AssetContentType` Enum

```csharp
public enum AssetContentType
{
    File,           // Generic uploaded file
    Image,          // Image with thumbnail support
    Link,           // URL bookmark
    Color,          // Hex color swatch
    ColorGroup,     // Group of color swatches
    Folder          // Organizational folder
}
```

Với `EnumMappings` class cung cấp bidirectional mapping giữa enum ↔ DB string (lowercase) để backward-compatible với data cũ.

---

## §5 — Design Patterns Used

| Pattern | Component | Vấn đề giải quyết |
|---------|-----------|-------------------|
| **TPH Inheritance** | `Asset` → 5 subtypes | Polymorphic entity với single DB table — không cần JOIN |
| **Factory Method** | `AssetFactory` (8 static methods) | Tạo đúng TPH subtype, encapsulate creation logic |
| **Template Method** | `Asset.ToDto()`, `HasPhysicalFile` | Shared mapping logic, type-specific behavior qua override |
| **Strategy** (planned) | Asset validation per type | Type-specific validation rules |
| **CQRS** | `Commands/` + `Queries/` (MediatR) | Tách read/write operations, mỗi use case 1 handler |
| **Mediator** | MediatR `IRequest`/`IRequestHandler` | Decouple Controller khỏi Service |
| **Observer** | SignalR notification on CRUD | Real-time UI update khi asset thay đổi |
| **ISP Split** | `IAssetService` + `IBulkAssetService` | Tách single-asset ops khỏi bulk ops |

---

## §6 — Luồng xử lý (Sequence Logic)

### 6.1 Upload Files (Primary Use Case)

**Trigger**: User drag-drop files vào canvas  
**Endpoint**: `POST /api/v1/assets/upload`

```
User (Browser)     React App          Controller           MediatR            AssetService         StorageService      ThumbnailSvc        DbContext
    │                │                    │                    │                    │                    │                  │                  │
    │── Drop files ─→│                    │                    │                    │                    │                  │                  │
    │                │── POST /upload ───→│                    │                    │                    │                  │                  │
    │                │   (FormData)       │                    │                    │                    │                  │                  │
    │                │                    │── UploadFiles ────→│                    │                    │                  │                  │
    │                │                    │   Command          │── Handle() ───────→│                    │                  │                  │
    │                │                    │                    │                    │                    │                  │                  │
    │                │                    │                    │                    │── foreach file ───→│                  │                  │
    │                │                    │                    │                    │   UploadAsync()    │                  │                  │
    │                │                    │                    │                    │←─── filePath ──────│                  │                  │
    │                │                    │                    │                    │                    │                  │                  │
    │                │                    │                    │                    │── if IsImage ─────→│─────────────────→│                  │
    │                │                    │                    │                    │   GenerateThumb()  │                  │                  │
    │                │                    │                    │                    │←── sm/md/lg paths──│──────────────────│                  │
    │                │                    │                    │                    │                    │                  │                  │
    │                │                    │                    │                    │── AssetFactory ───→│                  │                  │
    │                │                    │                    │                    │   .CreateImage()   │                  │                  │
    │                │                    │                    │                    │── asset.SetThumb() │                  │                  │
    │                │                    │                    │                    │                    │                  │                  │
    │                │                    │                    │                    │─── AddRange() ────→│──────────────────│──────────────────→│
    │                │                    │                    │                    │   SaveChanges()    │                  │                  │
    │                │                    │                    │                    │←─── saved ─────────│──────────────────│──────────────────│
    │                │                    │                    │                    │                    │                  │                  │
    │                │                    │                    │                    │── SignalR notify ──│──────────────────│──────────────────│
    │                │                    │                    │                    │                    │                  │                  │
    │                │                    │                    │←── List<DTO> ──────│                   │                  │                  │
    │                │                    │←── List<DTO> ──────│                    │                    │                  │                  │
    │                │←── HTTP 200 ───────│                    │                    │                    │                  │                  │
    │←── Render ─────│                    │                    │                    │                    │                  │                  │
```

**Chi tiết từng bước:**

| Step | Component | Action | Pattern |
|------|-----------|--------|---------|
| 1 | `AssetsCommandController` | Parse `FormData`, extract `IFormFile[]` | Presentation |
| 2 | Controller | Create `UploadFilesCommand` record | CQRS Command |
| 3 | `UploadFilesHandler` | Delegate to `IAssetService.UploadFilesAsync()` | Mediator |
| 4 | `AssetService` | Call `IStorageService.UploadAsync()` per file | DIP |
| 5 | `AssetService` | Check MIME type → if image, call `IThumbnailService.GenerateThumbnailsAsync()` | Polymorphism |
| 6 | `AssetService` | Call `AssetFactory.CreateImage()` or `.CreateFile()` | Factory |
| 7 | `AssetService` | `asset.SetThumbnails(sm, md, lg)` | Domain Method |
| 8 | `AssetService` | `_context.Assets.AddRange()` + `SaveChangesAsync()` | Unit of Work |
| 9 | `AssetService` | `INotificationService.NotifyAssetChanged()` | Observer |
| 10 | Return | `List<AssetResponseDto>` via `asset.ToDto()` | Template Method |

### 6.2 Delete Asset (with Cleanup)

**Trigger**: User click delete button  
**Endpoint**: `DELETE /api/v1/assets/{id}`

```
Controller          MediatR            AssetService         AssetCleanup       StorageService      ThumbnailSvc        DbContext
    │                  │                    │                    │                   │                  │                  │
    │── DeleteAsset ──→│                    │                    │                   │                  │                  │
    │   Command        │─── Handle() ──────→│                    │                   │                  │                  │
    │                  │                    │── FindAsync(id) ──→│───────────────────│──────────────────│─────────────────→│
    │                  │                    │←── asset ──────────│───────────────────│──────────────────│──────────────────│
    │                  │                    │                    │                   │                  │                  │
    │                  │                    │─── IsOwnedBy() ───→│ (domain check)    │                  │                  │
    │                  │                    │                    │                   │                  │                  │
    │                  │                    │── if RequiresFile─→│                   │                  │                  │
    │                  │                    │   Cleanup          │── DeleteAsync() ─→│                  │                  │
    │                  │                    │                    │                   │                  │                  │
    │                  │                    │── if CanHaveThumb─→│                   │                  │                  │
    │                  │                    │                    │── DeleteThumbs() ─│─────────────────→│                  │
    │                  │                    │                    │                   │                  │                  │
    │                  │                    │── Remove(asset) ──→│───────────────────│──────────────────│──────────────────→│
    │                  │                    │   SaveChanges()    │                   │                  │                  │
    │                  │                    │                    │                   │                  │                  │
    │                  │                    │── SignalR notify ──│                   │                  │                  │
    │                  │                    │                    │                   │                  │                  │
    │←── HTTP 200 ─────│←─── true ──────────│                    │                   │                  │                  │
```

**Key OOP Decisions:**

1. **`asset.RequiresFileCleanup`** — Polymorphic check. `ImageAsset` returns `true`, `LinkAsset` returns `false`. Service KHÔNG cần biết asset type cụ thể.
2. **`asset.CanHaveThumbnails`** — Chỉ `ImageAsset` override = `true`. Thumbnail cleanup tự động bỏ qua cho non-image types.
3. **`asset.IsOwnedBy(userId)`** — Domain method encapsulate ownership check. Nếu business rule thay đổi (team ownership), chỉ sửa 1 method.

### 6.3 Create Color Asset (Domain-specific)

**Trigger**: User tạo color swatch từ color picker  
**Endpoint**: `POST /api/v1/assets/color`

```
Controller          AssetService         AssetFactory        DbContext
    │                    │                    │                  │
    │── CreateColor ────→│                    │                  │
    │   (dto, userId)    │                    │                  │
    │                    │── CreateColor() ──→│                  │
    │                    │   (static factory) │                  │
    │                    │←── ColorAsset ─────│                  │
    │                    │                    │                  │
    │                    │── Add(asset) ─────→│─────────────────→│
    │                    │   SaveChanges()    │                  │
    │                    │                    │                  │
    │                    │── SignalR notify ──│                  │
    │                    │                    │                  │
    │←── AssetResponse ──│                    │                  │
    │   Dto              │                    │                  │
```

**Tại sao dùng `AssetFactory.CreateColor()` thay vì `new ColorAsset { ... }`?**
- Factory set `ContentType = AssetContentType.Color` → EF Core map đúng TPH discriminator
- Factory set `CreatedAt = DateTime.UtcNow` → consistent timestamp
- Factory set `UserId` → không ai quên ownership
- Caller không cần biết internal properties nào cần set

### 6.4 Update Position (Canvas Layout)

**Trigger**: User drag asset trên canvas  
**Endpoint**: `PUT /api/v1/assets/{id}/position`

```
Controller              AssetService         Asset (Entity)     DbContext
    │                        │                    │                 │
    │── UpdatePosition ─────→│                    │                 │
    │   (id, x, y, userId)   │                    │                 │
    │                        │── FindAsync(id) ──→│────────────────→│
    │                        │←── asset ──────────│                 │
    │                        │                    │                 │
    │                        │── IsOwnedBy() ────→│                 │
    │                        │                    │                 │
    │                        │── UpdatePosition()→│ (domain method) │
    │                        │   (x, y)           │                 │
    │                        │                    │                 │
    │                        │── SaveChanges() ──→│────────────────→│
    │                        │                    │                 │
    │←── AssetResponseDto ───│                    │                 │
```

**Encapsulation point**: Position thay đổi qua `asset.UpdatePosition(x, y)` — KHÔNG qua `asset.PositionX = x`. Domain method có thể thêm validation (bounds check) mà không sửa Service.

---

## §7 — API Endpoints

### 7.1 Query Endpoints

| Method | Route | Handler | Description |
|--------|-------|---------|-------------|
| `GET` | `/api/v1/assets` | `GetAssetsQuery` | Paginated list, filtered by user |
| `GET` | `/api/v1/assets/{id}` | `GetAssetByIdQuery` | Single asset by ID |
| `GET` | `/api/v1/assets/group/{groupId}` | `GetAssetsByGroupQuery` | Assets in color group |

### 7.2 Command Endpoints

| Method | Route | Handler | Description |
|--------|-------|---------|-------------|
| `POST` | `/api/v1/assets` | `CreateAssetCommand` | Create generic asset |
| `POST` | `/api/v1/assets/upload` | `UploadFilesCommand` | Upload file(s) |
| `POST` | `/api/v1/assets/folder` | `CreateAssetCommand` | Create folder |
| `POST` | `/api/v1/assets/color` | `CreateAssetCommand` | Create color swatch |
| `POST` | `/api/v1/assets/color-group` | `CreateAssetCommand` | Create color group |
| `POST` | `/api/v1/assets/link` | `CreateAssetCommand` | Create link/bookmark |
| `PUT` | `/api/v1/assets/{id}` | `UpdateAssetCommand` | Update asset properties |
| `PUT` | `/api/v1/assets/{id}/position` | Direct → `IAssetService` | Update canvas position |
| `POST` | `/api/v1/assets/reorder` | Direct → `IAssetService` | Reorder asset sequence |
| `POST` | `/api/v1/assets/{id}/duplicate` | `DuplicateAssetCommand` | Clone asset |
| `DELETE` | `/api/v1/assets/{id}` | `DeleteAssetCommand` | Delete asset + cleanup |

### 7.3 Bulk Endpoints

| Method | Route | Handler | Description |
|--------|-------|---------|-------------|
| `POST` | `/api/v1/bulk-assets/delete` | `IBulkAssetService` | Batch delete |
| `POST` | `/api/v1/bulk-assets/move` | `IBulkAssetService` | Batch move to collection/folder |
| `POST` | `/api/v1/bulk-assets/move-group` | `IBulkAssetService` | Batch move to color group |
| `POST` | `/api/v1/bulk-assets/tag` | `IBulkAssetService` | Batch add/remove tags |

---

## §8 — DTOs

### 8.1 Request DTOs

```csharp
public record CreateAssetDto(string FileName, string FilePath, int CollectionId, int? ParentFolderId);
public record CreateFolderDto(string Name, int CollectionId, int? ParentFolderId);
public record CreateColorDto(string ColorCode, int CollectionId, string? ColorName, int? GroupId, int? ParentFolderId);
public record CreateColorGroupDto(string GroupName, int CollectionId, int? ParentFolderId);
public record CreateLinkDto(string Name, string Url, int CollectionId, int? ParentFolderId);
public record UpdateAssetDto(string? FileName, int? SortOrder, int? GroupId, bool? ClearGroup, int? ParentFolderId, bool? ClearParentFolder);
public record AssetPositionDto(double PositionX, double PositionY);
public record ReorderAssetsDto(List<int> AssetIds);

// Bulk DTOs
public record BulkMoveDto(List<int> AssetIds, int TargetCollectionId, int? TargetFolderId);
public record BulkMoveGroupDto(List<int> AssetIds, int? TargetGroupId);
public record BulkTagDto(List<int> AssetIds, List<int> TagIds, string Action); // "add" | "remove"
```

### 8.2 Response DTOs

```csharp
public record AssetResponseDto
{
    public int Id { get; init; }
    public string FileName { get; init; }
    public string FilePath { get; init; }
    public string Tags { get; init; }
    public DateTime CreatedAt { get; init; }
    public double PositionX { get; init; }
    public double PositionY { get; init; }
    public int CollectionId { get; init; }
    public AssetContentType ContentType { get; init; }
    public int? GroupId { get; init; }
    public int? ParentFolderId { get; init; }
    public int SortOrder { get; init; }
    public bool IsFolder { get; init; }
    public string? ThumbnailSm { get; init; }
    public string? ThumbnailMd { get; init; }
    public string? ThumbnailLg { get; init; }
}

public record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize);
```

---

## §9 — Dependencies

### 9.1 This Module Depends On:

| Module/Service | Interface | Reason |
|---------------|-----------|--------|
| **Infrastructure** | `AppDbContext` | Data persistence (EF Core) |
| **Storage Module** | `IStorageService` | Upload/delete physical files |
| **Storage Module** | `IThumbnailService` | Generate image thumbnails |
| **Realtime Module** | `INotificationService` | Push CRUD events via SignalR |
| **ASP.NET Identity** | `ClaimTypes.NameIdentifier` | Extract `userId` from JWT |

### 9.2 Depended On By:

| Module/Service | Reason |
|---------------|--------|
| **Collection Module** | Asset belongs to Collection (FK relationship) |
| **Tag Module** | Assets have tags (M:N relationship via AssetTag) |
| **Search Module** | Search indexes assets |
| **Smart Collection Module** | Dynamic queries filter assets |
| **Permission Module** | Permission checks on asset operations |
| **Frontend** | `AssetsApi` class consumes all endpoints |

---

## §10 — Testing Strategy

| Test Type | Target | Coverage | Tools |
|-----------|--------|----------|-------|
| **Unit Tests** | `AssetFactory` (all 8 methods) | Correct subtype, correct defaults | xUnit |
| **Unit Tests** | `Asset` domain methods | `UpdatePosition`, `ApplyUpdate`, `IsOwnedBy` | xUnit |
| **Unit Tests** | `AssetService` (mocked dependencies) | Business logic, edge cases | xUnit, Moq |
| **Integration Tests** | API endpoints (14 endpoints) | Full pipeline, DB integration | WebApplicationFactory |
| **Integration Tests** | TPH serialization | Correct discriminator per subtype | SQLite in-memory |

**Priority Test Cases:**

| # | Test Case | Validates |
|---|-----------|-----------|
| 1 | Upload image → thumbnail generated | `HasPhysicalFile` + `CanHaveThumbnails` polymorphism |
| 2 | Upload non-image → no thumbnail | `CanHaveThumbnails = false` |
| 3 | Delete image → file + thumbnails cleaned | `RequiresFileCleanup` polymorphism |
| 4 | Delete link → no file cleanup | `HasPhysicalFile = false` |
| 5 | `AssetFactory.CreateImage()` → returns `ImageAsset` | Factory correctness |
| 6 | `AssetFactory.Duplicate()` → correct subtype | Switch expression coverage |
| 7 | Unauthorized user → exception | `IsOwnedBy()` guard |

---

## §11 — Known Issues & Technical Debt

| # | Issue | Severity | Planned Fix |
|---|-------|----------|-------------|
| 1 | `Asset` properties dùng `public set` thay vì `private set` | Medium | Phase: Stabilize (strict encapsulation) |
| 2 | Chưa có Repository pattern — Service depend trực tiếp `AppDbContext` | Medium | Phase: Modularize (`IAssetRepository`) |
| 3 | `AssetService` ~280 LOC — gần threshold 300 | Low | Monitor, split nếu thêm methods |
| 4 | Zero unit test coverage | **Critical** | Phase: Stabilize (priority #1) |
| 5 | `Tags` property là `string` (legacy) — nên dùng `AssetTag` M:N only | Low | Migration khi deprecate legacy tags |
| 6 | `ToDto()` mapping nằm trong Entity — nên tách qua AutoMapper hoặc Extension | Low | Phase: Modularize |
| 7 | `IAssetService` có 14 methods — gần threshold 15 | Low | Monitor, potential ISP split |
| 8 | `AssetFactory.Duplicate()` dùng switch trên `ContentType` enum | Low | Refactor sang virtual `Clone()` method |

---

> **Document End**  
> Template: [MODULE_TEMPLATE.md](MODULE_TEMPLATE.md)  
> Architecture: [ARCHITECTURE_CONVENTIONS.md](../01_DESIGN_PHILOSOPHY/ARCHITECTURE_CONVENTIONS.md)  
> Related Modules: [COLLECTION_MODULE.md](COLLECTION_MODULE.md) · [STORAGE_MODULE.md](STORAGE_MODULE.md) · [TAG_MODULE.md](TAG_MODULE.md)
