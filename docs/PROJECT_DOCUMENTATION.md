# Visual Asset Hub (VAH) — Tài liệu Kỹ thuật Chi tiết

> **Cập nhật:** 28/02/2026 — Phản ánh đầy đủ trạng thái hiện tại của dự án

---

## 2. Backend — `VAH.Backend/`

### 2.1 Cấu hình dự án (`VAH.Backend.csproj`)

- **Target:** .NET 9.0 (`net9.0`), Nullable enabled, ImplicitUsings enabled
- **NuGet packages (11):**
  - `Microsoft.AspNetCore.Authentication.JwtBearer` 9.*
  - `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 9.*
  - `Microsoft.EntityFrameworkCore.Design` 9.*
  - `Microsoft.EntityFrameworkCore.Sqlite` 9.*
  - `Microsoft.Extensions.Caching.StackExchangeRedis` 10.0.3
  - `Npgsql.EntityFrameworkCore.PostgreSQL` 9.*
  - `Serilog.AspNetCore` 10.0.0
  - `Serilog.Sinks.Console` 6.1.1
  - `Serilog.Sinks.File` 7.0.0
  - `SixLabors.ImageSharp` 3.1.12
  - `Swashbuckle.AspNetCore` 10.1.4 (Swagger)

### 2.2 Cấu hình ứng dụng (`appsettings.json`)

- Chuỗi kết nối: `Data Source=vah_database.db` (SQLite file trong thư mục gốc dự án)
- Logging: Default=Information, AspNetCore=Warning

### 2.3 Launch Settings (`Properties/launchSettings.json`)

- Profile `http`: `http://localhost:5027`, `ASPNETCORE_ENVIRONMENT=Development`
- Profile `https`: `https://localhost:7042` + `http://localhost:5027`

### 2.4 Entry Point (`Program.cs`)

- **Dịch vụ đã đăng ký:** Tổ chức qua `ServiceCollectionExtensions` extension methods:
  - `AddCorsPolicy()` — CORS (config-driven origins)
  - `AddRateLimitingPolicies()` — Rate Limiting (Fixed + Upload)
  - `AddDatabase()` — EF Core với SQLite/PostgreSQL dual-provider
  - `AddIdentityAndAuth()` — ASP.NET Identity + JWT Bearer Authentication
  - `AddCachingServices()` — Redis / In-memory distributed cache
  - `AddApplicationServices()` — Asset, Collection, Search, Storage, Auth, Tag, Thumbnail, Notification, SmartCollection, Permission services
  - Controllers + `JsonStringEnumConverter(KebabCaseLower)`, Swagger, SignalR
- **Logic khởi động:**
  - `Database.Migrate()` — tự động apply migrations khi khởi động
  - **Seed 3 collection mặc định** (trong migration) nếu chưa có:
    1. `Images` (type=`image`, color=`#007bff`, order=1)
    2. `Links` (type=`link`, color=`#28a745`, order=2)
    3. `Colors` (type=`color`, color=`#ffc107`, order=3)
- **Middleware pipeline:** GlobalExceptionHandler → CORS → RateLimiter → StaticFiles → Swagger (dev only) → Authentication → Authorization → MapControllers

---

### 2.5 Mô hình dữ liệu (Data Models)

#### `Models/Asset.cs` — Tài nguyên

| Thuộc tính | Kiểu | Mặc định | Mô tả |
|------------|------|----------|-------|
| `Id` | int | tự tăng | Khóa chính |
| `FileName` | string (Required) | `""` | Tên file gốc |
| `FilePath` | string (Required) | `""` | Đường dẫn tương đối (VD: `/uploads/guid.ext`) hoặc mã màu hoặc URL |
| `Tags` | string | `""` | Tags phân cách bằng dấu phẩy |
| `CreatedAt` | DateTime | `UtcNow` | Thời gian tạo |
| `PositionX` | double | `0` | Tọa độ X trên canvas kéo thả |
| `PositionY` | double | `0` | Tọa độ Y trên canvas kéo thả |
| `CollectionId` | int | `1` | FK đến Collection |
| `ContentType` | `AssetContentType` enum | `File` | `Image`, `Link`, `Color`, `Folder`, `ColorGroup`. DB lưu string, EF Core value conversion. TPH discriminator. |
| `GroupId` | int? | `null` | Dùng cho nhóm màu |
| `ParentFolderId` | int? | `null` | FK đến thư mục cha (hỗ trợ thư mục lồng nhau) |
| `SortOrder` | int | `0` | Thứ tự hiển thị |
| `IsFolder` | bool | `false` | Phân biệt thư mục và file |
| `UserId` | string? | `null` | FK đến `AspNetUsers`. `null` = system asset |

**TPH subtypes:** `ImageAsset`, `LinkAsset`, `ColorAsset`, `ColorGroupAsset`, `FolderAsset` (trong `AssetTypes.cs`)

**Navigation properties:** `Collection`, `ParentFolder`, `AssetTags`

**Domain methods:** `UpdatePosition()`, `ApplyUpdate(dto)`, `SetThumbnails()`, `MoveToFolder()`, `MoveToCollection()`, `IsOwnedBy(userId)`

**Virtual behavior:** `HasPhysicalFile`, `CanHaveThumbnails`, `RequiresFileCleanup`, `IsSystemAsset`

#### `Models/Collection.cs` — Bộ sưu tập

| Thuộc tính | Kiểu | Mặc định | Mô tả |
|------------|------|----------|-------|
| `Id` | int | tự tăng | Khóa chính |
| `Name` | string (Required) | `""` | Tên bộ sưu tập |
| `Description` | string | `""` | Mô tả |
| `ParentId` | int? | `null` | Collection cha (hỗ trợ cây phân cấp) |
| `CreatedAt` | DateTime | `UtcNow` | Thời gian tạo |
| `Color` | string | `"#007bff"` | Màu hiển thị trên UI |
| `Type` | `CollectionType` enum | `Default` | `Default`, `Image`, `Link`, `Color`. DB lưu string, EF Core value conversion. |
| `Order` | int | `0` | Thứ tự sắp xếp |
| `LayoutType` | `LayoutType` enum | `Grid` | `Grid`, `List`, `Canvas`. DB lưu string, EF Core value conversion. |
| `UserId` | string? | `null` | FK đến `AspNetUsers`. `null` = system collection |

**Navigation properties:** `Assets`, `Parent`, `Children`

**Domain methods:** `IsOwnedBy(userId)`, `IsAccessibleBy(userId)`, `ApplyUpdate(source)`

**Computed:** `IsSystemCollection`

#### `Models/DTOs.cs` — 6 DTO

| DTO | Trường | Mục đích |
|-----|--------|----------|
| `CreateFolderDto` | FolderName, CollectionId, ParentFolderId? | Tạo thư mục mới |
| `CreateColorDto` | ColorCode, ColorName?, CollectionId, GroupId?, SortOrder?, ParentFolderId? | Tạo mẫu màu |
| `UpdateAssetDto` | FileName?, SortOrder?, GroupId?, ParentFolderId?, ClearParentFolder? | Cập nhật một phần asset |
| `CreateLinkDto` | Name, Url, CollectionId, ParentFolderId? | Tạo liên kết/bookmark |
| `CreateColorGroupDto` | GroupName, CollectionId, ParentFolderId?, SortOrder? | Tạo nhóm màu |
| `ReorderAssetsDto` | AssetIds (List\<int\>) | Sắp xếp lại hàng loạt |

### 2.6 Database Context (`Data/AppDbContext.cs`)

- Kế thừa `IdentityDbContext<ApplicationUser>` (ASP.NET Identity)
- **242 dòng**, Fluent API configuration
- **5 DbSets:** `Assets`, `Collections`, `Tags`, `AssetTags`, `CollectionPermissions` (Identity tables được quản lý bởi base class)
- TPH discriminator config cho 6 Asset subtypes
- Seed data: 3 default collections (Images, Links, Colors)
- Dual-dialect SQL (PostgreSQL + SQLite) qua `DatabaseProviderInfo`
- 22 indexes cho tất cả entity tables

---

## 2. Models (Entities)

### 2.1 Asset

| Property | Type | Constraints | Default | Mô tả |
|----------|------|-------------|---------|-------|
| `Id` | int | PK, auto-increment | — | |
| `FileName` | string | Required, MaxLength(500) | `""` | Tên file hoặc folder |
| `FilePath` | string | Required, MaxLength(2048) | `""` | Đường dẫn file hoặc URL |
| `Tags` | string | MaxLength(2000) | `""` | Legacy comma-separated tags |
| `CreatedAt` | DateTime | | DB default | |
| `PositionX` | double | | `0` | Vị trí X trên canvas |
| `PositionY` | double | | `0` | Vị trí Y trên canvas |
| `CollectionId` | int | FK→Collections | `1` | |
| `ContentType` | `AssetContentType` enum | EF Core value conversion (DB lưu string) | `File` | `Image`, `Link`, `Color`, `Folder`, `ColorGroup`. TPH discriminator |
| `GroupId` | int? | | null | Nhóm màu (color collections) |
| `ParentFolderId` | int? | | null | Thư mục cha (self-ref) |
| `SortOrder` | int | | `0` | Thứ tự hiển thị |
| `IsFolder` | bool | | `false` | Là thư mục? |
| `UserId` | string? | FK→AspNetUsers | null | Data ownership |
| `ThumbnailSm` | string? | MaxLength(2048) | null | Thumbnail 150px WebP |
| `ThumbnailMd` | string? | MaxLength(2048) | null | Thumbnail 400px WebP |
| `ThumbnailLg` | string? | MaxLength(2048) | null | Thumbnail 800px WebP |
| `AssetTags` | ICollection\<AssetTag\> | Navigation | `[]` | M2M tags |

### 2.2 Collection

| Property | Type | Constraints | Default | Mô tả |
|----------|------|-------------|---------|-------|
| `Id` | int | PK | — | |
| `Name` | string | Required, MaxLength(255) | `""` | |
| `Description` | string | MaxLength(2000) | `""` | |
| `ParentId` | int? | FK→Collections (self) | null | Collection cha |
| `CreatedAt` | DateTime | | DB default | |
| `Color` | string | MaxLength(20) | `"#007bff"` | Màu hiển thị |
| `Type` | string | MaxLength(50) | `"default"` | image/link/color/default |
| `Order` | int | | `0` | Thứ tự sắp xếp |
| `LayoutType` | string | MaxLength(20) | `"grid"` | grid/list/masonry |
| `UserId` | string? | FK→AspNetUsers | null | null = system collection |

### 2.3 ApplicationUser (extends IdentityUser)

| Property | Type | Default | Mô tả |
|----------|------|---------|-------|
| `DisplayName` | string | `""` | Tên hiển thị |
| `CreatedAt` | DateTime | `DateTime.UtcNow` | |
| + tất cả fields từ IdentityUser (Email, PasswordHash, ...) | | | |

### 2.4 Tag

| Property | Type | Constraints | Mô tả |
|----------|------|-------------|-------|
| `Id` | int | PK | |
| `Name` | string | Required, MaxLength(100) | Tên gốc |
| `NormalizedName` | string | Required, MaxLength(100) | Lowercase, trimmed |
| `Color` | string? | MaxLength(20) | Màu badge |
| `UserId` | string? | FK→AspNetUsers | |
| `CreatedAt` | DateTime | DB default | |
| `AssetTags` | ICollection\<AssetTag\> | Navigation, JsonIgnore | |

### 2.5 AssetTag (Junction Table)

| Property | Type | Mô tả |
|----------|------|-------|
| `AssetId` | int | FK→Assets, part of composite PK |
| `TagId` | int | FK→Tags, part of composite PK |
| `Asset` | Asset? | Navigation, JsonIgnore |
| `Tag` | Tag? | Navigation |

### 2.6 CollectionPermission

| Property | Type | Constraints | Mô tả |
|----------|------|-------------|-------|
| `Id` | int | PK | |
| `UserId` | string | Required | FK→AspNetUsers |
| `CollectionId` | int | FK→Collections | |
| `Role` | string | Required, MaxLength(20) | owner/editor/viewer |
| `GrantedBy` | string? | | User ID của người cấp |
| `GrantedAt` | DateTime | DB default | |

**CollectionRoles (static class):**
- `Owner` — toàn quyền (read/write/manage/delete)
- `Editor` — đọc + ghi (read/write)
- `Viewer` — chỉ đọc (read only)
- `CanWrite(role)` → true nếu Owner hoặc Editor
- `CanManage(role)` → true nếu Owner

---

## 3. DTOs

### Auth DTOs

| DTO | Fields |
|-----|--------|
| `RegisterDto` | DisplayName (Required), Email (Required, EmailAddress), Password (Required, MinLength 6) |
| `LoginDto` | Email (Required), Password (Required) |
| `AuthResponseDto` | Token, Expiration, UserId, Email, DisplayName |

### Asset DTOs

| DTO | Fields |
|-----|--------|
| `CreateFolderDto` | FolderName (Required), CollectionId, ParentFolderId? |
| `CreateColorDto` | ColorCode (Required), ColorName?, CollectionId, GroupId?, SortOrder?, ParentFolderId? |
| `UpdateAssetDto` | FileName?, SortOrder?, GroupId?, ParentFolderId?, ClearParentFolder? |
| `CreateLinkDto` | Name (Required), Url (Required), CollectionId, ParentFolderId? |
| `CreateColorGroupDto` | GroupName (Required), CollectionId, ParentFolderId?, SortOrder? |
| `ReorderAssetsDto` | AssetIds (List\<int\>, Required) |
| `AssetPositionDto` | PositionX, PositionY |

### Tag DTOs

| DTO | Fields |
|-----|--------|
| `CreateTagDto` | Name (Required), Color? |
| `UpdateTagDto` | Name?, Color? |
| `AssetTagsDto` | TagIds (List\<int\>, Required) |

### Bulk DTOs

| DTO | Fields |
|-----|--------|
| `BulkDeleteDto` | AssetIds (List\<int\>, Required) |
| `BulkMoveDto` | AssetIds (Required), TargetCollectionId?, TargetFolderId?, ClearParentFolder? |
| `BulkMoveGroupDto` | AssetIds (List\<int\>, Required), TargetGroupId (int?), InsertBeforeId (int?) |
| `BulkTagDto` | AssetIds (Required), TagIds (Required), Remove (bool, default false) |

### Permission DTOs

| DTO | Fields |
|-----|--------|
| `GrantPermissionDto` | UserEmail (Required), Role (Required, MaxLength 20) |
| `UpdatePermissionDto` | Role (Required, MaxLength 20) |
| `PermissionInfoDto` | Id, UserId, UserEmail?, DisplayName?, Role, GrantedAt |

### Common DTOs

| DTO | Fields |
|-----|--------|
| `PagedResult<T>` | Items, TotalCount, Page, PageSize, HasNextPage, HasPreviousPage, TotalPages |
| `PaginationParams` | Page (default 1), PageSize (default 50, max 100), SortBy?, SortOrder (default "asc") |
| `FileUploadConfig` | MaxFileSizeBytes (50MB), MaxFilesPerRequest (20), AllowedExtensions (27), AllowedMimeTypePrefixes (13) |
| `SmartCollectionDefinition` | Id, Name, Description, Icon, Color, Count |

---

## 4. Services

### 4.1 AssetService (~280 lines)

**Dependencies:** AppDbContext, IStorageService, FileUploadConfig, IThumbnailService, INotificationService, ILogger

| Method | Return | Mô tả |
|--------|--------|-------|
| `GetAssetsAsync(PaginationParams, userId)` | `PagedResult<Asset>` | Phân trang, sắp xếp, user-scoped |
| `GetByIdAsync(id, userId)` | `Asset?` | |
| `CreateAssetAsync(Asset, userId)` | `Asset` | + SignalR notify |
| `UploadFilesAsync(files, collectionId, folderId, userId)` | `List<Asset>` | Validate size/ext/MIME, AssetFactory, thumbnail gen |
| `UpdatePositionAsync(id, x, y, userId)` | `Asset` | Canvas position (domain method) |
| `CreateFolderAsync(dto, userId)` | `Asset` | AssetFactory.CreateFolder |
| `CreateColorAsync(dto, userId)` | `Asset` | Auto-prepend `#` cho hex codes, AssetFactory.CreateColor |
| `CreateColorGroupAsync(dto, userId)` | `Asset` | AssetFactory.CreateColorGroup |
| `CreateLinkAsync(dto, userId)` | `Asset` | URL validation (http/https), AssetFactory.CreateLink |
| `UpdateAssetAsync(id, dto, userId)` | `Asset` | Partial update (domain method ApplyUpdate) |
| `DeleteAssetAsync(id, userId)` | `bool` | AssetCleanupHelper for file + thumbnails |
| `ReorderAssetsAsync(ids, userId)` | `void` | Batch SortOrder |
| `GetAssetsByGroupAsync(groupId, userId)` | `List<Asset>` | |
| `BulkMoveGroupAsync(dto, userId)` | `int` | Move colors to group with positional insert |

### 4.2 BulkAssetService (NEW)

**Dependencies:** AppDbContext, AssetCleanupHelper, INotificationService, ILogger

| Method | Return | Mô tả |
|--------|--------|-------|
| `BulkDeleteAsync(ids, userId)` | `int` | Bulk delete files + thumbnails via AssetCleanupHelper |
| `BulkMoveAsync(dto, userId)` | `int` | Validate target collection, batch move |
| `BulkTagAsync(dto, userId)` | `int` | Add/remove tags batch |
| `BulkMoveGroupAsync(dto, userId)` | `int` | Move colors between groups with positional insert |

### 4.3 AssetCleanupHelper (NEW)

**Dependencies:** IStorageService, IWebHostEnvironment, ILogger

| Method | Return | Mô tả |
|--------|--------|-------|
| `CleanupFilesAsync(asset)` | `void` | Xóa file vật lý nếu `RequiresFileCleanup` |
| `CleanupThumbnailsAsync(asset)` | `void` | Xóa 3 thumbnail sizes |

Dùng trong cả `AssetService` và `BulkAssetService` (DRY principle).

### 4.4 CollectionService (264 lines)

**Dependencies:** AppDbContext, ILogger, IDistributedCache, INotificationService, IPermissionService

| Method | Return | Mô tả |
|--------|--------|-------|
| `GetAllAsync(userId)` | `List<Collection>` | Cached (5min/2min), own + system + shared |
| `GetByIdAsync(id, userId)` | `Collection?` | Permission-aware |
| `GetWithItemsAsync(id, folderId, userId)` | `CollectionWithItemsResult` | Items + SubCollections, permission check |
| `CreateAsync(collection, userId)` | `Collection` | Invalidate cache, notify |
| `UpdateAsync(id, collection, userId)` | `Collection` | Editor+ permission cho shared |
| `DeleteAsync(id, userId)` | `bool` | Owner only, orphan prevention |

### 4.3 AuthService (115 lines)

**Dependencies:** UserManager\<ApplicationUser\>, IConfiguration, ILogger

| Method | Return | Mô tả |
|--------|--------|-------|
| `RegisterAsync(dto)` | `AuthResponseDto` | UserManager.CreateAsync + JWT |
| `LoginAsync(dto)` | `AuthResponseDto` | Validate credentials + JWT |

**JWT Claims:** NameIdentifier, Email, Name (DisplayName), Jti (GUID)

### 4.4 LocalStorageService (82 lines)

| Method | Return | Mô tả |
|--------|--------|-------|
| `UploadAsync(stream, fileName, contentType)` | `string` | GUID naming, trả `/uploads/{guid}.ext` |
| `DeleteAsync(filePath)` | `bool` | Xóa file vật lý |
| `GetPublicUrl(path)` | `string` | Trả path as-is |
| `Exists(path)` | `bool` | File.Exists check |

### 4.5 ThumbnailService (113 lines)

| Method | Return | Mô tả |
|--------|--------|-------|
| `GenerateThumbnailsAsync(originalPath)` | `Dictionary<string,string>` | sm(150px), md(400px), lg(800px) WebP |

**Formats hỗ trợ:** jpg, jpeg, png, gif, bmp, webp, tiff  
**Output:** `/uploads/thumbs/{size}_{fileId}.webp`, quality 80

### 4.6 TagService (281 lines)

**Dependencies:** AppDbContext, ILogger

| Method | Return | Mô tả |
|--------|--------|-------|
| `GetAllAsync(userId)` | `List<Tag>` | |
| `GetByIdAsync(id, userId)` | `Tag` | |
| `CreateAsync(dto, userId)` | `Tag` | Dedup by normalized name |
| `UpdateAsync(id, dto, userId)` | `Tag` | |
| `DeleteAsync(id, userId)` | `bool` | |
| `GetOrCreateTagsAsync(names, userId)` | `List<Tag>` | Batch get-or-create |
| `SetAssetTagsAsync(assetId, tagIds, userId)` | `void` | Replace all + sync legacy field |
| `AddAssetTagsAsync(assetId, tagIds, userId)` | `void` | Additive |
| `RemoveAssetTagsAsync(assetId, tagIds, userId)` | `void` | |
| `GetAssetTagsAsync(assetId, userId)` | `List<Tag>` | |
| `MigrateCommaSeparatedTagsAsync(userId)` | `void` | One-time migration |

### 4.7 NotificationService (35 lines)

| Method | Return | Mô tả |
|--------|--------|-------|
| `NotifyAsync(userId, eventType, payload)` | `void` | Gửi tới group `user:{userId}`, silent error |

### 4.10 SmartCollectionService (198 lines)

> **Refactored:** Sử dụng Strategy pattern qua `ISmartCollectionFilter` interface.

| Method | Return | Mô tả |
|--------|--------|-------|
| `GetDefinitionsAsync(userId)` | `List<SmartCollectionDefinition>` | 8 built-in + top 10 per-tag |
| `GetItemsAsync(id, params, userId)` | `PagedResult<Asset>` | Delegate tới ISmartCollectionFilter strategies |

**Strategy classes (trong SmartCollectionFilters.cs):**

| Strategy | Loại | Mô tả |
|----------|------|-------|
| `RecentDaysFilter` | Built-in | Assets tạo trong N ngày qua |
| `ContentTypeFilter` | Built-in | Assets theo content type (image/link/color) |
| `UntaggedFilter` | Built-in | Assets chưa gắn tag |
| `WithThumbnailsFilter` | Built-in | Assets có thumbnail |
| `TagFilter` | Dynamic | Assets theo tag cụ thể |

**Built-in smart collections:**

| ID | Tên | Mô tả |
|----|-----|-------|
| `recent-7d` | Gần đây (7 ngày) | Assets tạo trong 7 ngày |
| `recent-30d` | Gần đây (30 ngày) | Assets tạo trong 30 ngày |
| `all-images` | Tất cả hình ảnh | ContentType = image |
| `all-links` | Tất cả liên kết | ContentType = link |
| `all-colors` | Tất cả màu sắc | ContentType = color |
| `untagged` | Chưa gắn tag | Không có AssetTag nào |
| `with-thumbnails` | Có thumbnail | ThumbnailSm != null |
| `tag-{id}` | Theo tag | Assets có tag cụ thể |

### 4.9 PermissionService (228 lines)

**Dependencies:** AppDbContext, UserManager, ILogger

| Method | Return | Mô tả |
|--------|--------|-------|
| `HasPermissionAsync(collectionId, userId, minRole)` | `bool` | Owner luôn có full access |
| `GetRoleAsync(collectionId, userId)` | `string?` | |
| `GrantAsync(collectionId, dto, grantedBy)` | `CollectionPermission` | Owner only, upsert, by email |
| `UpdateAsync(permId, dto, userId)` | `CollectionPermission` | Owner only |
| `RevokeAsync(permId, userId)` | `bool` | Owner only |
| `ListAsync(collectionId, userId)` | `List<PermissionInfoDto>` | Viewer+ access |
| `GetSharedCollectionsAsync(userId)` | `List<Collection>` | Collections shared with user |

---

## 5. Controllers — 38 Endpoints

### 5.1 AssetsController — 15 endpoints

```
[Authorize] api/Assets

GET    /                      → PagedResult<Asset>   (PaginationParams)
POST   /                      → Asset                (Asset body)
POST   /upload                → List<Asset>          (FormData files, ?collectionId, ?folderId)
PUT    /{id}/position         → Asset                (AssetPositionDto)
POST   /create-folder         → Asset                (CreateFolderDto)
POST   /create-color          → Asset                (CreateColorDto)
POST   /create-color-group    → Asset                (CreateColorGroupDto)
POST   /create-link           → Asset                (CreateLinkDto)
PUT    /{id}                  → Asset                (UpdateAssetDto)
DELETE /{id}                  → 204
POST   /reorder               → 200                  (ReorderAssetsDto)
GET    /group/{groupId}       → List<Asset>
POST   /bulk-delete            → {deleted: int}       (BulkDeleteDto)
POST   /bulk-move              → {moved: int}         (BulkMoveDto)
POST   /bulk-move-group        → {moved: int}         (BulkMoveGroupDto)
POST   /bulk-tag               → {affected: int}      (BulkTagDto)
```

### 5.2 AuthController — 2 endpoints

```
[RateLimited("Fixed")] api/Auth

POST   /register              → AuthResponseDto       (RegisterDto)
POST   /login                 → AuthResponseDto       (LoginDto)
```

### 5.3 CollectionsController — 5 endpoints

```
[Authorize] api/Collections

GET    /                      → List<Collection>
GET    /{id}/items             → CollectionWithItemsResult  (?folderId)
POST   /                      → Collection             (Collection body)
PUT    /{id}                  → 204                    (Collection body)
DELETE /{id}                  → 204
```

### 5.4 SearchController — 1 endpoint

```
[Authorize] api/Search

GET    /?q=&type=&collectionId=&page=&pageSize=  → SearchResult
```

### 5.5 TagsController — 10 endpoints

```
[Authorize] api/Tags

GET    /                      → List<Tag>
GET    /{id}                  → Tag
POST   /                      → Tag                   (CreateTagDto)
PUT    /{id}                  → Tag                   (UpdateTagDto)
DELETE /{id}                  → 204
GET    /asset/{assetId}       → List<Tag>
PUT    /asset/{assetId}       → 200                   (AssetTagsDto — replace all)
POST   /asset/{assetId}/add   → 200                   (AssetTagsDto — add)
POST   /asset/{assetId}/remove → 200                  (AssetTagsDto — remove)
POST   /migrate               → {message}             (migrate legacy tags)
```

### 5.6 SmartCollectionsController — 2 endpoints

```
[Authorize] api/SmartCollections

GET    /                      → List<SmartCollectionDefinition>
GET    /{id}/items             → PagedResult<Asset>    (PaginationParams)
```

### 5.7 PermissionsController — 6 endpoints

```
[Authorize] api/collections/{collectionId}/permissions

GET    /                      → List<PermissionInfoDto>
POST   /                      → CollectionPermission   (GrantPermissionDto)
PUT    /{permissionId}        → CollectionPermission   (UpdatePermissionDto)
DELETE /{permissionId}        → 204

GET    /my-role               → {role: string}

[Authorize] api/shared-collections
GET    /                      → List<Collection>
```

### 5.8 HealthController — 1 endpoint

```
[No Auth] api/Health

## 8. Cấu trúc thư mục dự án
```text
1A/
├── VAH.sln                          # Solution file
├── README.md                        # Hướng dẫn cài đặt & sử dụng
├── docs/                            # Tài liệu dự án
│   ├── PROJECT_DOCUMENTATION.md     # Tài liệu này
│   ├── ARCHITECTURE_REVIEW.md       # Đánh giá kiến trúc & roadmap
│   └── IMPLEMENTATION_GUIDE.md      # Hướng dẫn triển khai canvas
│
├── VAH.Backend/                     # ASP.NET Core Web API
│   ├── Program.cs                   # Entry point, cấu hình services
│   ├── VAH.Backend.csproj           # Cấu hình dự án .NET
│   ├── appsettings.json             # Cấu hình (connection string, logging)
│   ├── Controllers/                 # 9 controllers, ~485 dòng tổng
│   │   ├── BaseApiController.cs     # Abstract base controller (GetUserId()) — 18 dòng
│   │   ├── AssetsController.cs      # REST API cho assets (12 endpoints) — 124 dòng
│   │   ├── AuthController.cs        # Login/Register — 33 dòng
│   │   ├── CollectionsController.cs # REST API cho collections (5 endpoints) — 51 dòng
│   │   ├── HealthController.cs      # Health check (1 endpoint) — 50 dòng
│   │   ├── PermissionsController.cs # Sharing permissions — 57 dòng
│   │   ├── SearchController.cs      # Thin delegate → SearchService — 34 dòng
│   │   ├── SmartCollectionsController.cs # Smart collections — 33 dòng
│   │   └── TagsController.cs        # Tag CRUD — 85 dòng
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs  # DI registration (6 methods) — 182 dòng
│   ├── Services/                    # 24 files (10 interfaces + 12 implementations + 2 helpers), ~1,800 dòng tổng
│   │   ├── IAssetService.cs / AssetService.cs        # Asset CRUD — ~280 dòng (dùng AssetFactory + domain methods)
│   │   ├── IBulkAssetService.cs / BulkAssetService.cs # Bulk delete/move/tag — NEW (tách từ AssetService)
│   │   ├── AssetCleanupHelper.cs                    # File + thumbnail cleanup utility — NEW
│   │   ├── IAuthService.cs / AuthService.cs          # Authentication — ~90 dòng
│   │   ├── ICollectionService.cs / CollectionService.cs  # Collection CRUD — ~264 dòng
│   │   ├── IPermissionService.cs / PermissionService.cs  # Permission management — ~228 dòng
│   │   ├── ISearchService.cs / SearchService.cs      # Search (extracted from controller) — ~71 dòng
│   │   ├── ISmartCollectionService.cs / SmartCollectionService.cs  # Smart queries — ~198 dòng
│   │   ├── SmartCollectionFilters.cs                # ISmartCollectionFilter + 5 Strategy classes — ~191 dòng NEW
│   │   ├── ITagService.cs / TagService.cs            # Tag management — ~281 dòng
│   │   ├── IThumbnailService.cs / ThumbnailService.cs  # Thumbnail gen — ~113 dòng
│   │   ├── IStorageService.cs / LocalStorageService.cs  # File storage — ~82 dòng
│   │   └── INotificationService.cs / NotificationService.cs  # SignalR — ~35 dòng
│   ├── Models/                      # 11 files, ~750 dòng tổng
│   │   ├── Asset.cs                 # TPH base class — ~94 dòng (enums, virtual behavior, domain methods, nav props)
│   │   ├── AssetTypes.cs            # TPH subtypes: ImageAsset, LinkAsset, ColorAsset, ColorGroupAsset, FolderAsset — ~32 dòng
│   │   ├── AssetFactory.cs          # Factory pattern — 6 static Create methods (ContentType được set) — ~74 dòng
│   │   ├── Enums.cs                 # AssetContentType, CollectionType, LayoutType + EnumMappings — ~99 dòng
│   │   ├── Collection.cs            # Collection model (domain methods, nav props) — ~54 dòng
│   │   ├── CollectionPermission.cs  # Computed (CanWrite, CanManage) + SetRole — ~68 dòng
│   │   ├── Tag.cs                   # Domain methods (SetName, UpdateFrom, IsOwnedBy) — ~54 dòng
│   │   ├── ApplicationUser.cs       # Identity user — ~10 dòng
│   │   ├── DTOs.cs                  # Data Transfer Objects (incl. BulkMoveGroupDto) — ~191 dòng
│   │   ├── AuthDTOs.cs              # Auth request/response DTOs — ~26 dòng
│   │   └── Common.cs                # PagedResult, PaginationParams, FileUploadConfig — ~52 dòng
│   ├── Data/
│   │   └── AppDbContext.cs          # EF Core DbContext + Fluent API config
│   ├── Middleware/
│   │   └── ExceptionHandlingMiddleware.cs  # Global exception handler (RFC 7807)
│   ├── Migrations/                  # 5 EF Core migrations
│   │   ├── *_InitialCreate.cs       # Assets, Collections, Identity tables
│   │   ├── *_AddThumbnailColumns.cs  # ThumbnailSm, ThumbnailMd, ThumbnailLg
│   │   ├── *_AddTagSystem.cs        # Tags, AssetTags (M2M junction)
│   │   ├── *_AddCollectionPermissions.cs # CollectionPermissions (RBAC)
│   │   ├── *_SyncModelChanges.cs    # FK_Assets_Assets_ParentFolderId
│   │   └── AppDbContextModelSnapshot.cs
│   ├── Properties/
│   │   └── launchSettings.json      # Cấu hình chạy
│   └── wwwroot/uploads/             # Thư mục lưu file upload
│
└── VAH.Frontend/                    # React + Vite SPA
    ├── package.json                 # Dependencies & scripts
    ├── vite.config.js               # Vite configuration
    ├── index.html                   # HTML entry point
    └── src/
        ├── main.jsx                 # StrictMode → ErrorBoundary → BrowserRouter → AuthProvider → App
        ├── App.jsx                  # AppLayout + Routes (344 lines, refactored từ 620)
        ├── App.css                  # Dark Navy theme (24 CSS variables)
        ├── index.css                # Global reset & base styles
        ├── api/                     # 11 files (class-based OOP architecture)
        │   ├── BaseApiService.js    # Abstract base class (_get/_post/_put/_delete)
        │   ├── TokenManager.js      # JWT token singleton (private #storageKey)
        │   ├── client.js            # Axios instance + JWT interceptor + staticUrl
        │   ├── assetsApi.js         # AssetApiService extends BaseApiService (16 methods)
        │   ├── authApi.js           # AuthApiService extends BaseApiService
        │   ├── collectionsApi.js    # CollectionApiService extends BaseApiService
        │   ├── searchApi.js         # SearchApiService extends BaseApiService
        │   ├── tagsApi.js           # TagApiService extends BaseApiService (10 methods)
        │   ├── smartCollectionsApi.js # SmartCollectionApiService extends BaseApiService
        │   ├── permissionsApi.js    # PermissionApiService extends BaseApiService
        │   └── index.js             # Barrel file — re-exports all service singletons
        ├── hooks/                   # 11 custom hooks
        │   ├── useAuth.js           # AuthProvider context + login/register/logout
        │   ├── useAssets.js         # CRUD + compose useAssetSelection + useBulkOperations
        │   ├── useAssetSelection.js # Multi-select state: toggle, range, selectAll
        │   ├── useBulkOperations.js # Bulk delete/move/tag/moveGroup
        │   ├── useCollections.js    # State + CRUD + compose useCollectionNavigation
        │   ├── useCollectionNavigation.js # URL sync, breadcrumbs, folder path
        │   ├── useSmartCollections.js # Smart collection fetch + state
        │   ├── useSharePermissions.js # Permission CRUD (grant/updateRole/revoke)
        │   ├── useTags.js           # Tag CRUD + asset-tag M2M
        │   ├── useSignalR.js        # Real-time connection + events
        │   └── useUndoRedo.js       # Command pattern (50 history)
        ├── context/                 # State management
        │   └── AppContext.js        # AppProvider + useAppContext() — centralised state
        ├── models/                  # Domain model classes
        │   └── index.js             # Asset, Collection, Tag classes + mapping helpers
        └── components/              # 14 components
            ├── AppHeader.jsx            # Header bar (search, actions, notifications)
            ├── AppSidebar.jsx           # Sidebar (collection tree, smart collections)
            ├── DetailsPanel.jsx         # Right panel (preview, metadata, tags)
            ├── LoginPage.jsx/css          # Login/Register form
            ├── ErrorBoundary.jsx          # React error boundary (class component)
            ├── CollectionTree.jsx/css     # Sidebar tree navigation
            ├── CollectionBrowser.jsx/css  # File browser (grid/list/masonry)
            ├── AssetDisplayer.jsx/css     # Gallery / Canvas view
            ├── AssetGrid.jsx/css          # Lưới thumbnail cơ bản
            ├── SearchBar.jsx/css          # Thanh tìm kiếm
            ├── UploadArea.jsx/css         # Vùng upload kéo thả
            ├── ColorBoard.jsx/css         # Bảng màu (drag-drop, copy, multi-select)
            ├── ShareDialog.jsx/css        # RBAC sharing dialog (presentational)
            └── DraggableAssetCanvas.jsx/css # Canvas kéo thả tự do
```

---

## 6. Frontend — API Layer

### 6.1 client.js — Axios Instance

- **Base URL:** `VITE_API_URL` hoặc `http://localhost:5027/api`
- **Timeout:** 30s
- **Request interceptor:** Gắn `Authorization: Bearer <token>` từ localStorage
- **Response interceptor:** Auto-clear token + reload khi 401
- **Exports:** `default` (axios), `getToken`, `setToken`, `clearToken`, `STATIC_URL`, `staticUrl(path)`

### 6.2 API Modules

| Module | Functions |
|--------|-----------|
| `assetsApi.js` | uploadFiles, createFolder, createLink, createColor, createColorGroup, updateAsset, updatePosition, deleteAsset, reorderAssets, bulkDelete, bulkMove, bulkTag |
| `authApi.js` | register(displayName, email, password), login(email, password) |
| `collectionsApi.js` | fetchAllCollections, fetchCollectionItems(id, folderId?), createCollection, deleteCollection |
| `searchApi.js` | search({q, type, collectionId, page, pageSize}) |
| `tagsApi.js` | fetchAllTags, fetchTag, createTag, updateTag, deleteTag, getAssetTags, setAssetTags, addAssetTags, removeAssetTags, migrateTags |
| `smartCollectionsApi.js` | fetchSmartCollections, fetchSmartCollectionItems(id, page?, pageSize?) |
| `permissionsApi.js` | fetchPermissions, grantPermission, updatePermission, revokePermission, getMyRole, fetchSharedCollections |

---

## 7. Frontend — Hooks

### 7.1 useAuth

**Provider:** `AuthProvider` (context)  
**Returns:** `{ user, isAuthenticated, authLoading, authError, login, register, logout, setAuthError }`  
**Persist:** `vah_token` + `vah_user` trong localStorage

### 7.2 useCollections

**Returns:** `{ collections, selectedCollection, collectionItems, loading, breadcrumbPath, folderPath, currentFolderId, selectCollection, breadcrumbClick, navigateToCollection, openFolder, folderBreadcrumbClick, folderBreadcrumbRoot, handleCreateCollection, handleDeleteCollection, refreshItems, setSelectedCollection }`  
**URL Sync:** `useParams()` cho collectionId/folderId, `useNavigate()` push URL

### 7.3 useAssets

**Input:** `{ selectedCollection, currentFolderId, collectionItems, refreshItems }`  
**Returns:** `{ selectedAssetId, setSelectedAssetId, selectedAsset, selectedAssetIds, toggleSelectAsset, selectAllAssets, clearSelection, handleUpload, handleCreateFolder, handleCreateLink, handleCreateColorGroup, handleCreateColor, handleMoveAsset, handleMoveSelected, handleReorderAssets, handleBulkDelete, handleBulkMove, handleBulkTag }`  
**Multi-select:** Ctrl+click (toggle), Shift+click (range), normal (single)

### 7.4 useTags

**Returns:** `{ tags, loading, fetchTags, createTag, updateTag, deleteTag, getAssetTags, setAssetTags, addAssetTags, removeAssetTags }`  
Auto-fetch on mount.

### 7.5 useSignalR

**Input:** `(handlers, enabled)`  
**Returns:** `{ connection }`  
**Hub URL:** `/hubs/assets` + JWT token  
**Auto-reconnect:** [0, 2s, 5s, 10s, 30s]  
**10 event types:** AssetCreated, AssetUpdated, AssetDeleted, AssetsUploaded, AssetsBulkDeleted, AssetsBulkMoved, CollectionCreated, CollectionUpdated, CollectionDeleted, TagsChanged

### 7.6 useUndoRedo

**Input:** `(maxHistory = 50)`  
**Returns:** `{ execute, undo, redo, canUndo, canRedo, history }`  
**Command:** `{ execute: fn, undo: fn, description: string }`  
**Keyboard:** Ctrl+Z (undo), Ctrl+Shift+Z (redo)

### 7.7 useAssetSelection

**Returns:** `{ selectedAssetId, setSelectedAssetId, selectedAsset, selectedAssetIds, toggleSelectAsset, selectAllAssets, clearSelection }`  
**Multi-select:** Ctrl+click (toggle), Shift+click (range), normal (single).

### 7.8 useBulkOperations

**Input:** `{ selectedAssetIds, selectedCollection, currentFolderId, refreshItems, clearSelection }`  
**Returns:** `{ handleBulkDelete, handleBulkMove, handleBulkTag, handleBulkMoveGroup }`  
Batch API calls for multi-select operations.

### 7.9 useCollectionNavigation

**Returns:** `{ breadcrumbPath, folderPath, currentFolderId, navigateToCollection, openFolder, folderBreadcrumbClick, folderBreadcrumbRoot, breadcrumbClick, syncFromUrl, syncInitial }`  
**URL Sync:** `useParams()` cho collectionId/folderId, `useNavigate()` push URL.

### 7.10 useSmartCollections

**Returns:** `{ smartCollections, selectedSmartCollection, selectSmartCollection, smartCollectionItems, smartLoading }`  
Auto-fetch smart collection definitions on mount.

### 7.11 useSharePermissions

**Input:** `(collectionId)`  
**Returns:** `{ permissions, loading, error, grant, updateRole, revoke }`  
Permission CRUD extracted từ ShareDialog. `grant(email, role)`, `updateRole(permId, newRole)`, `revoke(permId)`.

---

## 8. Frontend — Components

| Component | Props | Chức năng |
|-----------|-------|-----------|
| `AppHeader` | (từ useAppContext) | Header bar: search, view mode toggle, actions, notifications |
| `AppSidebar` | (từ useAppContext) | Sidebar: collection tree, smart collections, share indicator |
| `DetailsPanel` | (từ useAppContext) | Right panel: asset preview, metadata, tag management |
| `LoginPage` | — (sử dụng useAuth) | Login/Register form với toggle |
| `ErrorBoundary` | children | React error boundary, hiển thị fallback khi crash |
| `CollectionTree` | collections, selectedCollection, onSelectCollection, onCreateCollection, onDeleteCollection | Sidebar hierarchical tree, icons theo type |
| `CollectionBrowser` | assets, subCollections, onSelectCollection, onSelectFolder, onMoveAsset, onSelectAsset, selectedAssetId, selectedAssetIds, onReorder, loading, searchTerm, layoutMode | File browser chính: grid/list/masonry, drag-and-drop, reorder, search filter |
| `AssetDisplayer` | assets, subCollections, viewMode, onSelectCollection, loading | Gallery + canvas mode |
| `AssetGrid` | assets | Simple card grid |
| `UploadArea` | onUpload | react-dropzone file drop zone |
| `ColorBoard` | items, onCreateColor, onCreateGroup, onSelectAsset, onMoveColorsToGroup, selectedAssetIds | Color palette manager: group columns, click-to-copy hex code, drag-drop reorder with positional insert indicator, multi-select drag, drag handle (⠣) |
| `SearchBar` | onSearch | Search input |
| `ShareDialog` | collectionId, collectionName, onClose | RBAC sharing modal (presentational — logic trong useSharePermissions hook) |
| `DraggableAssetCanvas` | (internal) | Canvas drag-and-drop cho images |

---

### 9.6 THỐNG KÊ DỰ ÁN

> **Cập nhật:** Sau OOP refactoring 5/5 Phases hoàn tất (23/23 tasks)

#### Quy mô code — Backend

| Thành phần | Files | Dòng code |
|------------|-------|----------|
| Controllers | 9 | ~500 |
| Services (interfaces + impl + helpers) | 24 (10 interfaces + 12 impls + 2 helpers) | ~1,800 |
| Models / DTOs | 11 | ~750 |
| Extensions | 1 | ~182 |
| Data (AppDbContext) | 1 | ~242 |
| Middleware | 1 | ~71 |
| Hubs (SignalR) | 1 | ~52 |
| Program.cs | 1 | ~148 |
| **Tổng Backend** | **~49 files** | **~3,745** |

#### Quy mô code — Frontend

| Thành phần | Files | Dòng code (ước tính) |
|------------|-------|---------------------|
| Frontend Components | 14 (.jsx) + 10 (.css) | ~800 + ~800 |
| Frontend Hooks | 11 | ~700 |
| Frontend API (class-based) | 11 (7 domain + BaseApiService + TokenManager + client + barrel) | ~350 |
| Frontend Context | 1 (AppContext.js) | ~120 |
| Frontend Models | 1 (index.js: Asset, Collection, Tag) | ~180 |
| Frontend App | 1 (App.jsx) + 1 (App.css) | ~344 + ~400 |
| **Tổng Frontend** | **~50+ files** | **~3,694+** |

| | | |
|---|---|---|
| **TỔNG DỰ ÁN** | **~99+ files** | **~7,439+** |

#### API Endpoints (44 total)

| Controller | Endpoints | Methods |
|------------|-----------|---------|  
| Assets | 17 | GET(2), POST(9), PUT(2), DELETE(1), bulk ops(3) |
| Collections | 5 | GET(2), POST(1), PUT(1), DELETE(1) |
| Auth | 2 | POST(2) |
| Tags | 10 | GET(3), POST(3), PUT(2), DELETE(1), POST migrate(1) |
| Permissions | 6 | GET(3), POST(1), PUT(1), DELETE(1) |
| SmartCollections | 2 | GET(2) |
| Search | 1 | GET(1) |
| Health | 1 | GET(1) |

#### Database

| Bảng | Mô tả | FK |
|------|-------|-----|
| Assets | TPH (ContentType discriminator) → 6 subtypes | → Collections, → ParentFolder (self) |
| Collections | Enum Type/LayoutType, nav props | → Parent (self-ref) |
| Tags | Normalized name, color | → User |
| AssetTags | Many-to-many junction | → Assets, → Tags |
| CollectionPermissions | Role-based sharing | → Collections, → User |
| AspNetUsers | ASP.NET Identity | — |

### 9.2 Docker Compose Environment

```yaml
backend:
  ASPNETCORE_ENVIRONMENT: Production
  DatabaseProvider: PostgreSQL
  ConnectionStrings__DefaultConnection: Host=postgres;Database=vah;...
  ConnectionStrings__Redis: redis:6379
  Jwt__SecretKey: ...(production key)...
  Cors__AllowedOrigins__0: http://localhost:3000

frontend:
  VITE_API_URL: http://backend:5027/api
```

---

### 9.8 KẾT LUẬN

Dự án Visual Asset Hub đã trải qua **4 giai đoạn phát triển**, **OOP Refactoring 5/5 Phases** hoàn chỉnh (23/23 tasks), và **Session #4** bổ sung UX features. Backend hiện có:
- **TPH Inheritance** (Asset → 5 subtypes) với virtual behavior properties
- **Factory Pattern** (AssetFactory) cho type-safe creation với ContentType được set đúng
- **Rich Domain Model** (domain methods trên 4 entities)
- **Type-safe Enums** với EF Core value conversions
- **Navigation Properties** cho quan hệ parent-child
- **Base Controller** giảm duplication
- **Extension Methods** tổ chức DI registration (Program.cs: 255→148 dòng)
- **BulkAssetService** tách khỏi AssetService (ISP)
- **AssetCleanupHelper** encapsulate cleanup logic (SRP)
- **Strategy Pattern** cho SmartCollectionService (5 filter strategies, OCP)
- **Shared-Collection Access Control**: Tất cả CRUD operations kiểm tra permission qua `IPermissionService`
- **Duplicate Asset API**: `POST /api/assets/{id}/duplicate` — clone với đúng TPH subtype
- **Permission Cache Invalidation**: Tự động xóa cache khi thay đổi permission
- **Class-based Frontend API** layer (BaseApiService → 7 subclasses + TokenManager singleton)

Frontend hiện có:
- **Domain Model Classes**: `Asset`, `Collection`, `Tag` với computed properties và validation
- **AppContext + AppProvider**: Centralised state management (Context API, 387 dòng)
- **ConfirmContext + ConfirmProvider**: Promise-based confirm/prompt/alert thay thế `window.confirm/prompt/alert`
- **Component Decomposition**: `AppHeader`, `AppSidebar`, `DetailsPanel` tách từ App.jsx
- **17 Components** bao gồm 3 mới: `ContextMenu` (right-click), `ConfirmDialog` (styled dialogs), `TreeViewPanel` (right sidebar tree)
- **Hook Composition**: 11 hooks chuyên biệt
- **Clipboard System**: Copy/Cut/Paste cho assets (Duplicate hoặc Move)
- **Pin System**: Ghim items nhanh vào sidebar, persist localStorage
- **Folder Multi-Select**: Ctrl+click chọn nhiều thư mục + bulk delete
- **Global Image Paste**: Ctrl+V paste ảnh từ clipboard hệ thống → auto-upload
- **Context Menu**: Right-click trên mọi item với menu ngữ cảnh đầy đủ

**Tất cả phases đã hoàn tất. Không còn OOP tasks pending.**
