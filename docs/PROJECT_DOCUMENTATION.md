# Visual Asset Hub (VAH) — Tài liệu Kỹ thuật Chi tiết

> **Cập nhật:** 27/02/2026 — Phản ánh đầy đủ trạng thái hiện tại của dự án

---

## 2. Backend — `VAH.Backend/`

### 2.1 Cấu hình dự án (`VAH.Backend.csproj`)

- **Target:** .NET 9.0 (`net9.0`), Nullable enabled, ImplicitUsings enabled
- **NuGet packages:**
  - `Microsoft.EntityFrameworkCore.Design` 9.*
  - `Microsoft.EntityFrameworkCore.Sqlite` 9.*
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
  - Controllers, Swagger, SignalR
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
- **DbSets:** `Assets`, `Collections` (Identity tables được quản lý bởi base class)

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
| `ContentType` | string | MaxLength(50) | `"image"` | image/link/color/folder/file |
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

### 4.1 AssetService (518 lines)

**Dependencies:** AppDbContext, IStorageService, FileUploadConfig, IThumbnailService, INotificationService, ILogger

| Method | Return | Mô tả |
|--------|--------|-------|
| `GetAssetsAsync(PaginationParams, userId)` | `PagedResult<Asset>` | Phân trang, sắp xếp, user-scoped |
| `GetByIdAsync(id, userId)` | `Asset?` | |
| `CreateAssetAsync(Asset, userId)` | `Asset` | + SignalR notify |
| `UploadFilesAsync(files, collectionId, folderId, userId)` | `List<Asset>` | Validate size/ext/MIME, thumbnail gen |
| `UpdatePositionAsync(id, x, y, userId)` | `Asset` | Canvas position |
| `CreateFolderAsync(dto, userId)` | `Asset` | |
| `CreateColorAsync(dto, userId)` | `Asset` | |
| `CreateColorGroupAsync(dto, userId)` | `Asset` | |
| `CreateLinkAsync(dto, userId)` | `Asset` | URL validation (http/https) |
| `UpdateAssetAsync(id, dto, userId)` | `Asset` | Partial update |
| `DeleteAssetAsync(id, userId)` | `bool` | Xóa file + thumbnails, orphan prevention |
| `ReorderAssetsAsync(ids, userId)` | `void` | Batch SortOrder |
| `GetAssetsByGroupAsync(groupId, userId)` | `List<Asset>` | |
| `BulkDeleteAsync(ids, userId)` | `int` | Bulk cleanup |
| `BulkMoveAsync(dto, userId)` | `int` | Validate target collection |
| `BulkTagAsync(dto, userId)` | `int` | Add/remove tags batch |

### 4.2 CollectionService (264 lines)

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

### 4.8 SmartCollectionService (198 lines)

| Method | Return | Mô tả |
|--------|--------|-------|
| `GetDefinitionsAsync(userId)` | `List<SmartCollectionDefinition>` | 8 built-in + top 10 per-tag |
| `GetItemsAsync(id, params, userId)` | `PagedResult<Asset>` | Dynamic LINQ filter |

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
│   ├── Controllers/
│   │   ├── BaseApiController.cs     # Abstract base controller (GetUserId())
│   │   ├── AssetsController.cs      # REST API cho assets (12 endpoints)
│   │   ├── CollectionsController.cs # REST API cho collections (5 endpoints)
│   │   ├── SearchController.cs      # REST API tìm kiếm (thin delegate → SearchService)
│   │   └── HealthController.cs      # Health check (1 endpoint)
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs  # DI registration extension methods
│   ├── Services/
│   │   ├── IAssetService.cs         # Interface
│   │   ├── AssetService.cs          # Implementation (~480 dòng, dùng AssetFactory)
│   │   ├── ICollectionService.cs    # Interface
│   │   ├── CollectionService.cs     # Implementation (111 dòng)
│   │   ├── ISearchService.cs        # Interface
│   │   ├── SearchService.cs         # Search implementation (extracted from controller)
│   │   ├── IStorageService.cs       # Interface
│   │   └── LocalStorageService.cs   # Local file storage
│   ├── Models/
│   │   ├── Asset.cs                 # Base asset model (TPH base class)
│   │   ├── AssetTypes.cs            # TPH subtypes: ImageAsset, LinkAsset, ColorAsset, ColorGroupAsset, FolderAsset
│   │   ├── AssetFactory.cs          # Factory pattern cho tạo đúng subtype
│   │   ├── Enums.cs                 # AssetContentType, CollectionType, LayoutType + EnumMappings
│   │   ├── Collection.cs            # Model bộ sưu tập
│   │   ├── DTOs.cs                  # Data Transfer Objects (incl. SearchResult, AssetPositionDto, etc.)
│   │   └── Common.cs                # PagedResult, PaginationParams, FileUploadConfig
│   ├── Data/
│   │   └── AppDbContext.cs          # EF Core DbContext + Fluent API config
│   ├── Middleware/
│   │   └── ExceptionHandlingMiddleware.cs  # Global exception handler (RFC 7807)
│   ├── Migrations/                  # EF Core migration files
│   │   ├── *_InitialCreate.cs       # Initial schema migration
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
        ├── main.jsx                 # React entry point
        ├── App.jsx                  # Component chính (state, routing, layout)
        ├── App.css                  # Design system & layout styles
        ├── index.css                # Global reset & base styles
        ├── api/
        │   ├── client.js            # Axios instance + interceptors
        │   ├── assetsApi.js         # API functions cho assets
        │   ├── collectionsApi.js     # API functions cho collections
        │   └── searchApi.js          # API function cho search
        ├── hooks/
        │   ├── useCollections.js     # Collections state + CRUD
        │   └── useAssets.js          # Assets state + CRUD
        └── components/
            ├── CollectionTree.jsx/css     # Cây thư mục sidebar
            ├── CollectionBrowser.jsx/css  # Trình duyệt file chính
            ├── AssetDisplayer.jsx/css     # Gallery / Canvas view
            ├── AssetGrid.jsx/css          # Lưới thumbnail cơ bản
            ├── SearchBar.jsx/css          # Thanh tìm kiếm (phụ)
            ├── UploadArea.jsx/css         # Vùng upload kéo thả
            ├── ColorBoard.jsx/css         # Bảng quản lý màu
            ├── DraggableAssetCanvas.jsx/css # Canvas kéo thả tự do
            └── ErrorBoundary.jsx          # Error boundary component
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

---

## 8. Frontend — Components

| Component | Props | Chức năng |
|-----------|-------|-----------|
| `LoginPage` | — (sử dụng useAuth) | Login/Register form với toggle |
| `ErrorBoundary` | children | React error boundary, hiển thị fallback khi crash |
| `CollectionTree` | collections, selectedCollection, onSelectCollection, onCreateCollection, onDeleteCollection | Sidebar hierarchical tree, icons theo type |
| `CollectionBrowser` | assets, subCollections, onSelectCollection, onSelectFolder, onMoveAsset, onSelectAsset, selectedAssetId, selectedAssetIds, onReorder, loading, searchTerm, layoutMode | File browser chính: grid/list/masonry, drag-and-drop, reorder, search filter |
| `AssetDisplayer` | assets, subCollections, viewMode, onSelectCollection, loading | Gallery + canvas mode |
| `AssetGrid` | assets | Simple card grid |
| `UploadArea` | onUpload | react-dropzone file drop zone |
| `ColorBoard` | items, onCreateColor, onCreateGroup | Color palette manager, group columns |
| `SearchBar` | onSearch | Search input |
| `ShareDialog` | collectionId, collectionName, onClose | RBAC sharing modal: grant/update/revoke by email |
| `DraggableAssetCanvas` | (internal) | Canvas drag-and-drop cho images |

---

## 9. Cấu hình

### 9.1 appsettings.json

```json
{
  "DatabaseProvider": "SQLite",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=vah_database.db",
    "PostgreSQL": "Host=localhost;Port=5432;Database=vah;...",
    "Redis": ""
  },
  "Jwt": {
    "SecretKey": "...(32+ chars)...",
    "Issuer": "VAH.Backend",
    "Audience": "VAH.Frontend",
    "ExpirationHours": 24
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173", "http://localhost:5174"]
  }
}
```

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

## 10. File Upload Configuration

| Setting | Value |
|---------|-------|
| Max file size | 50 MB |
| Max files per request | 20 |
| Kestrel body limit | 100 MB |
| Allowed extensions | .jpg .jpeg .png .gif .bmp .webp .tiff .svg .ico .pdf .doc .docx .xls .xlsx .ppt .pptx .txt .csv .json .xml .zip .rar .mp4 .mp3 .wav .mov .avi |
| Allowed MIME prefixes | image/, video/, audio/, application/pdf, application/msword, application/vnd.openxmlformats, application/vnd.ms-, text/, application/json, application/xml, application/zip, application/x-rar |
| Thumbnail formats | jpg, jpeg, png, gif, bmp, webp, tiff |
| Thumbnail sizes | sm: 150px, md: 400px, lg: 800px |
| Thumbnail format | WebP, quality 80 |
| Storage path | wwwroot/uploads/ |
| Thumbnail path | wwwroot/uploads/thumbs/ |
