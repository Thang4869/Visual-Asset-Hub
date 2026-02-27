# Visual Asset Hub (VAH) — Tài liệu dự án

> Tài liệu mô tả toàn bộ kiến trúc, chức năng và thành phần của dự án Visual Asset Hub — ứng dụng web quản lý tệp tin và tài nguyên số.

## 1. Tổng quan kiến trúc

| Tầng | Công nghệ | Cổng |
|------|-----------|------|
| Backend | ASP.NET Core 9.0 (Web API) | `http://localhost:5027` |
| Frontend | React 19 + Vite 7 | `http://localhost:5173` |
| Cơ sở dữ liệu | SQLite (file: `vah_database.db`) | nhúng (embedded) |
| Giao tiếp | REST API thông qua `axios`; CORS policy "AllowAll" | — |

Frontend kết nối đến backend tại `http://localhost:5027/api` (hằng số `API_URL` trong `App.jsx`).

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
- **DbSets:** `Assets`, `Collections` (Identity tables được quản lý bởi base class)

---

### 2.7 Controllers & API Endpoints

#### `Controllers/AssetsController.cs` — Route: `api/Assets`

| # | Method | Route | Mục đích |
|---|--------|-------|----------|
| 1 | `GET` | `/api/Assets` | Lấy tất cả assets |
| 2 | `POST` | `/api/Assets` | Tạo asset (JSON body) |
| 3 | `POST` | `/api/Assets/upload?collectionId=&folderId=` | Upload file (multipart/form-data), lưu vào `wwwroot/uploads/`, đặt tên file bằng GUID |
| 4 | `PUT` | `/api/Assets/{id}/position` | Cập nhật vị trí X/Y (kéo thả canvas) |
| 5 | `POST` | `/api/Assets/create-folder` | Tạo thư mục |
| 6 | `POST` | `/api/Assets/create-color` | Tạo mẫu màu |
| 7 | `POST` | `/api/Assets/create-color-group` | Tạo nhóm màu |
| 8 | `POST` | `/api/Assets/create-link` | Tạo liên kết/bookmark |
| 9 | `PUT` | `/api/Assets/{id}` | Cập nhật một phần (tên, thứ tự, nhóm, thư mục) |
| 10 | `DELETE` | `/api/Assets/{id}` | Xóa asset |
| 11 | `POST` | `/api/Assets/reorder` | Sắp xếp lại assets hàng loạt |
| 12 | `GET` | `/api/Assets/group/{groupId}` | Lấy assets theo nhóm |

#### `Controllers/CollectionsController.cs` — Route: `api/Collections`

| Method | Route | Mục đích |
|--------|-------|----------|
| `GET` | `/api/Collections` | Danh sách tất cả collections, sắp xếp theo Order |
| `GET` | `/api/Collections/{id}/items?folderId=` | Lấy collection cùng items (lọc theo folderId) + subcollections. Trả về `CollectionWithItems`. Items sắp xếp: thư mục trước → SortOrder → FileName. **User-scoped** |
| `POST` | `/api/Collections` | Tạo collection mới (gán UserId tự động) |
| `PUT` | `/api/Collections/{id}` | Cập nhật collection |
| `DELETE` | `/api/Collections/{id}` | Xóa collection (chuyển collection con thành top-level) |

---

## 3. Frontend — `VAH.Frontend/`

### 3.1 Cấu hình dự án (`package.json`)

- **Tên:** `vah-frontend`, **Phiên bản:** `0.0.0`
- **Dependencies:** React 19.2, ReactDOM 19.2, axios 1.13.5, react-dropzone 15.0.0
- **DevDependencies:** Vite 7.3.1, @vitejs/plugin-react 5.1.1, ESLint 9.39.1
- **Scripts:** `dev` (vite), `build` (vite build), `lint` (eslint), `preview` (vite preview)

### 3.2 Cấu hình Vite (`vite.config.js`)

- Cấu hình Vite chuẩn với React plugin. Không có proxy được cấu hình.

### 3.3 Entry Point (`index.html`)

- **Tiêu đề:** "Visual Asset Hub"
- **Font:** Google Fonts `Inter` (weights 400, 500, 600, 700) tải non-blocking
- Mount React tại `<div id="root">`

### 3.4 Global Styles (`src/index.css`)

- Box-sizing reset, full-height html/body/#root
- **Font:** `'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif`
- **Background:** `#0a1929` (xanh lam đậm), **Text:** `#ffffff`
- Scrollbar tùy chỉnh: mỏng (6px), thumb bán trong suốt

---

### 3.5 Hệ thống thiết kế (`src/App.css`)

#### Bảng màu CSS Variables (`:root`)

| Biến | Giá trị | Mục đích |
|------|---------|----------|
| `--bg-darkest` | `#0a1929` | Nền trang |
| `--bg-dark` | `#0d2137` | Khu vực nội dung chính |
| `--bg-sidebar` | `#0b1a2e` | Nền sidebar |
| `--bg-header` | `#0b1929` | Nền header trên cùng |
| `--bg-card` | `#132f4c` | Nền thẻ (card) |
| `--bg-card-hover` | `#173a5e` | Nền thẻ khi hover |
| `--bg-input` | `#132f4c` | Nền trường nhập liệu |
| `--accent` | `#2196F3` | Màu điểm nhấn chính (Xanh dương sáng) |
| `--accent-hover` | `#1976D2` | Accent khi hover |
| `--accent-light` | `rgba(33,150,243,0.12)` | Nền accent nhẹ |
| `--text-primary` | `#ffffff` | Chữ chính (trắng) |
| `--text-secondary` | `#94a3b8` | Chữ phụ (xám nhạt) |
| `--text-muted` | `#546e7a` | Chữ mờ/nhãn |
| `--border-color` | `#1e3a5f` | Viền |
| `--border-subtle` | `rgba(255,255,255,0.06)` | Đường phân chia mờ |
| `--shadow-card` | `0 2px 8px rgba(0,0,0,0.25)` | Đổ bóng thẻ |
| `--shadow-card-hover` | `0 8px 24px rgba(0,0,0,0.4)` | Đổ bóng hover |
| `--danger` | `#ef5350` | Hành động xóa/nguy hiểm |
| `--notif-red` | `#f44336` | Chấm thông báo đỏ |
| `--radius` | `8px` | Bo góc chuẩn |
| `--radius-lg` | `12px` | Bo góc lớn |

#### Bố cục 4 khu vực

1. **Thanh Header trên cùng (56px)**: Logo + Hamburger menu (trái) → Thanh tìm kiếm dạng thuốc (giữa, max 520px) → Cụm icon chức năng + Avatar (phải)
2. **Sidebar trái (240px)**: Cây thư mục/collection phân cấp với expand/collapse
3. **Khu vực nội dung chính (flex)**: Toolbar (breadcrumbs + nút hành động + view switcher) → Vùng cuộn nội dung → Upload section
4. **Panel chi tiết phải (320px)**: Hiển thị có điều kiện khi chọn asset — preview, metadata, full preview

#### Hệ thống nút

- `.btn-primary`: Nền accent xanh, chữ trắng, padding 8px 18px
- `.btn-secondary`: Nền card, viền, chữ secondary, hover chuyển accent
- `.view-switcher`: Segmented control với icon list/grid/masonry

---

### 3.6 Component chính (`src/App.jsx`)

#### State Management

| State | Kiểu | Mục đích |
|-------|------|----------|
| `collections` | Array | Tất cả collections từ API |
| `selectedCollection` | Object\|null | Collection đang active |
| `collectionItems` | `{items, subCollections}` | Items của collection đang chọn |
| `viewMode` | string | `'browser'` hoặc `'canvas'` |
| `layoutMode` | string | `'grid'` / `'list'` / `'masonry'` |
| `loading` | bool | Chỉ thị đang tải |
| `breadcrumbPath` | Array | Đường dẫn điều hướng collection |
| `folderPath` | Array | Đường dẫn điều hướng thư mục |
| `currentFolderId` | int\|null | Thư mục đang mở |
| `searchTerm` | string | Bộ lọc tìm kiếm toàn cục |
| `selectedAssetId` | int\|null | Asset được chọn cho panel chi tiết |

#### Các hàm chính

| Hàm | Mục đích |
|-----|----------|
| `fetchCollections()` | GET `/api/Collections`, tự chọn collection đầu tiên |
| `fetchCollectionItems(id, folderId)` | GET `/api/Collections/{id}/items?folderId=` |
| `handleSelectCollection(collection, path)` | Điều hướng đến collection, reset state |
| `handleOpenFolder(folder)` | Mở thư mục, push vào folderPath |
| `handleUpload(files)` | POST multipart đến `/api/Assets/upload` |
| `handleCreateCollection(name, parentId)` | POST `/api/Collections`, hỏi type qua prompt |
| `handleDeleteCollection(id)` | DELETE với xác nhận |
| `handleCreateFolder()` | POST `/api/Assets/create-folder` qua prompt |
| `handleCreateLink()` | POST `/api/Assets/create-link` qua prompt |
| `handleCreateColorGroup()` | POST `/api/Assets/create-color-group` |
| `handleCreateColor(colorCode, groupId)` | POST `/api/Assets/create-color` |
| `handleMoveAsset(assetId, folderId)` | PUT `/api/Assets/{id}` với parentFolderId |
| `handleMoveSelected()` | Di chuyển asset được chọn (nhập tên folder hoặc "root") |
| `handleReorderAssets(assetIds)` | POST `/api/Assets/reorder` |

#### Logic hiển thị

- Nếu `selectedCollection.type === 'color'` → hiển thị `<ColorBoard>`
- Nếu `viewMode === 'browser'` → hiển thị `<CollectionBrowser>`
- Nếu `viewMode === 'canvas'` → hiển thị `<AssetDisplayer>` (chỉ ảnh)
- Panel chi tiết (phải): hiển thị khi có `selectedAsset` — thumbnail/icon, bảng metadata, preview đầy đủ cho ảnh

---

### 3.7 Các Component con

#### `CollectionTree` — Cây thư mục bên trái

- **Props:** `collections`, `selectedCollection`, `onSelectCollection`, `onCreateCollection`, `onDeleteCollection`
- Cây phân cấp với expand/collapse (▶/▼)
- Mục được chọn: nền accent xanh dương sáng + đổ bóng
- Icon theo type: 🖼️ (image), 🔗 (link), 🎨 (color), 📁 (default)
- Nút xóa (✕) xuất hiện khi hover

#### `CollectionBrowser` — Trình duyệt file chính

- **Props:** `assets`, `subCollections`, `onSelectCollection`, `onSelectFolder`, `onMoveAsset`, `onSelectAsset`, `selectedAssetId`, `onReorder`, `loading`, `searchTerm`, `layoutMode`
- Lọc tìm kiếm theo tên collection, thư mục, file
- Hai phần: "Thư mục" và "Tệp tin"
- **Layout modes:** `grid` (auto-fill 175px), `list` (flex column), `masonry` (CSS columns 4×200px)
- Kéo thả: kéo file vào thư mục để di chuyển
- File được chọn: viền accent + đổ bóng
- Nút sắp xếp lại (▲/▼) trong chế độ list
- Thumbnail ảnh, icon link (🔗), mẫu màu

#### `AssetDisplayer` — Gallery / Canvas view

- **Props:** `assets`, `subCollections`, `viewMode`, `onSelectCollection`, `loading`
- Thẻ subcollection với viền trên màu và icon type
- Gallery grid (minmax 180px) cho chế độ không phải canvas
- Chế độ canvas: ủy thác cho `<DraggableAssetCanvas>`
- Hỗ trợ: images, links, colors

#### `AssetGrid` — Lưới thumbnail cơ bản

- **Props:** `assets`
- Grid auto-fill (minmax 160px), hiển thị `thumbnailUrl`, fileName, tags
- Card hover với hiệu ứng nâng + viền accent

#### `SearchBar` — Thanh tìm kiếm (component phụ)

- **Props:** `onSearch`
- Input dạng thuốc 300px

#### `UploadArea` — Vùng upload kéo thả

- **Props:** `onUpload`
- Sử dụng `react-dropzone`
- Viền nét đứt, accent khi drag-active/hover

#### `ColorBoard` — Bảng quản lý bảng màu

- **Props:** `items`, `onCreateColor`, `onCreateGroup`
- Input nhập mã màu (VD: `#FFAA00`), Enter để tạo
- Dropdown chọn nhóm đích
- Nút "New group" tạo nhóm màu
- Các cột theo nhóm (auto-fit grid, minmax 220px)
- Mỗi màu: swatch (20×20) + mã monospace

#### `DraggableAssetCanvas` — Canvas kéo thả tự do

- **Props:** `assets`, `onPositionUpdate`
- Kéo thả bằng mouse events (mousedown → mousemove → mouseup)
- Giới hạn vị trí trong canvas
- Tự lưu vị trí qua `PUT /api/Assets/{id}/position` khi thả chuột
- Nền grid pattern (ô vuông 50px)
- Asset: thẻ 150px rộng, preview ảnh 110px + tên file

---

## 4. Tính năng chính

| # | Tính năng | Mô tả |
|---|-----------|-------|
| 1 | **Quản lý Collection** | Tạo, xóa, cây phân cấp cha/con; types: image, link, color, default |
| 2 | **Upload File** | Kéo thả qua react-dropzone, upload multipart đến `wwwroot/uploads/`, tên file GUID |
| 3 | **Hệ thống thư mục** | Thư mục lồng nhau trong collections (asset với `IsFolder=true`), breadcrumb navigation |
| 4 | **Hiển thị hình ảnh** | Grid/List/Masonry layouts + canvas kéo thả tự do với vị trí persistent |
| 5 | **Quản lý liên kết** | Lưu trữ bookmarks (tên + URL) |
| 6 | **Bảng màu** | Mẫu màu tổ chức theo nhóm, nhập bằng mã hex, hiển thị dạng cột Kanban |
| 7 | **Tìm kiếm** | Lọc phía client theo tên trên collections, thư mục và files |
| 8 | **Panel chi tiết Asset** | Sidebar phải với preview, bảng metadata, preview đầy đủ cho ảnh |
| 9 | **Kéo & thả** | Di chuyển file giữa thư mục bằng kéo thả; sắp xếp lại trong list view |
| 10 | **Responsive Layouts** | Grid (auto-fill), List (stacked rows), Masonry (CSS multi-column) |

---

## 5. Thiết kế giao diện (Design Theme)

| Khía cạnh         | Chi tiết                                                  |
|-------------------|-----------------------------------------------------------|
| **Chủ đề**        | Dark Navy / Material Dark                                 |
| **Phong cách**    | Chế độ tối, hiện đại, phẳng (flat design) với đổ bóng nhẹ |
| **Nền chính**     | `#0a1929` → `#0d2137` (xanh lam đậm + xám đen)            |
| **Bề mặt card**   | `#132f4c` / hover `#173a5e`                               |
| **Màu điểm nhấn** | `#2196F3` (Xanh dương sáng) / hover `#1976D2`             |
| **Typography**    | Inter (Google Fonts), weights 400–700, sans-serif         |
| **Bo góc**        | 8px chuẩn, 12px cho cards                                 |
| **Đổ bóng**       | Multi-layer box shadows                                   |
| **Icon**          | Inline SVG (Feather-style) + emoji (📁🖼️🔗🎨)           |
| **Scrollbar**     | Tùy chỉnh mỏng (6px), bán trong suốt                      |
| **Ngôn ngữ**      | Tiếng Việt cho nhãn UI                                    |

---

## 6. Cơ sở dữ liệu

- **Engine:** SQLite qua Entity Framework Core
- **File:** `vah_database.db` trong thư mục `VAH.Backend/`
- **Bảng:** `Assets`, `Collections`
- **Quản lý schema:** EF Core Migrations — `dotnet ef migrations add` / `dotnet ef database update`. Ứng dụng tự động chạy `db.Database.Migrate()` khi khởi động
- **Seed data:** 3 collections mặc định (Images, Links, Colors) qua `HasData()` trong `OnModelCreating`
- **Migration files:** `VAH.Backend/Migrations/`
- **Lưu ý:** `CreatedAt` sử dụng `HasDefaultValueSql("datetime('now')")` ở DB level; services set `DateTime.UtcNow` khi tạo entity

---

## 7. Lưu trữ file upload

- **Thư mục:** `VAH.Backend/wwwroot/uploads/`
- **Đặt tên:** GUID + extension gốc (VD: `a1b2c3d4-...-e5f6.png`)
- **Phục vụ:** Static files middleware từ đường dẫn `/uploads/`
- **Truy cập từ frontend:** `http://localhost:5027/uploads/{filename}`

---

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
│   ├── Services/                    # 22 files (11 interfaces + 11 implementations), ~1,674 dòng tổng
│   │   ├── IAssetService.cs / AssetService.cs        # Asset CRUD — 369 dòng (dùng AssetFactory + domain methods)
│   │   ├── IAuthService.cs / AuthService.cs          # Authentication — 90 dòng
│   │   ├── ICollectionService.cs / CollectionService.cs  # Collection CRUD — 177 dòng
│   │   ├── IPermissionService.cs / PermissionService.cs  # Permission management — 149 dòng
│   │   ├── ISearchService.cs / SearchService.cs      # Search (extracted from controller) — 71 dòng
│   │   ├── ISmartCollectionService.cs / SmartCollectionService.cs  # Smart queries — 178 dòng
│   │   ├── ITagService.cs / TagService.cs            # Tag management — 224 dòng
│   │   ├── IThumbnailService.cs / ThumbnailService.cs  # Thumbnail gen — 92 dòng
│   │   ├── IStorageService.cs / LocalStorageService.cs  # File storage — 61 dòng
│   │   └── INotificationService.cs / NotificationService.cs  # SignalR — 27 dòng
│   ├── Models/                      # 11 files, ~703 dòng tổng
│   │   ├── Asset.cs                 # TPH base class — 94 dòng (enums, virtual behavior, domain methods, nav props)
│   │   ├── AssetTypes.cs            # TPH subtypes: ImageAsset, LinkAsset, ColorAsset, ColorGroupAsset, FolderAsset — 32 dòng
│   │   ├── AssetFactory.cs          # Factory pattern — 6 static Create methods — 74 dòng
│   │   ├── Enums.cs                 # AssetContentType, CollectionType, LayoutType + EnumMappings — 99 dòng
│   │   ├── Collection.cs            # Collection model (domain methods, nav props) — 54 dòng
│   │   ├── CollectionPermission.cs  # Computed (CanWrite, CanManage) + SetRole — 68 dòng
│   │   ├── Tag.cs                   # Domain methods (SetName, UpdateFrom, IsOwnedBy) — 54 dòng
│   │   ├── ApplicationUser.cs       # Identity user — 10 dòng
│   │   ├── DTOs.cs                  # Data Transfer Objects — 140 dòng
│   │   ├── AuthDTOs.cs              # Auth request/response DTOs — 26 dòng
│   │   └── Common.cs                # PagedResult, PaginationParams, FileUploadConfig — 52 dòng
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

## 9. ĐÁNH GIÁ DỰ ÁN — Hiện trạng & Tiến độ thực hiện

> **Ngày đánh giá:** 25/02/2026  
> **Phạm vi:** So sánh hiện trạng thực tế của code với khuyến nghị trong ARCHITECTURE_REVIEW.md  
> **Mục tiêu:** Xác định rõ những gì đã làm, đang thiếu, và cần bổ sung

---

### 9.1 Tổng quan tiến độ

| Giai đoạn (theo ARCHITECTURE_REVIEW) | Tổng hạng mục | Đã hoàn thành | Chưa thực hiện | Tỷ lệ |
|---------------------------------------|--------------|---------------|----------------|--------|
| **GĐ 1 — Production cơ bản** | 7 | 7 | 0 | 100% |
| **GĐ 2 — Chuẩn hóa kiến trúc** | 6 | 5 | 1 | 83% |
| **GĐ 3 — Production-grade** | 5+ | 1 | 4+ | ~20% |
| **Bổ sung mới (không trong roadmap)** | 5 | 5 | 0 | 100% |

---

### 9.2 NHỮNG THỨ ĐÃ CÓ TRONG DỰ ÁN (Đã thực hiện)

#### A) Backend — Kiến trúc & Patterns

| # | Hạng mục | Thuộc giai đoạn | Chi tiết thực hiện |
|---|----------|-----------------|-------------------|
| 1 | **Service Layer** | GĐ 2.1 | ✅ Đã tách hoàn chỉnh: `IAssetService` / `AssetService` (328 dòng), `ICollectionService` / `CollectionService` (111 dòng). Controller chỉ còn nhận request + gọi service + trả response. Đúng mô hình Thin Controller → Service |
| 2 | **Storage Abstraction** | GĐ 2.4 | ✅ `IStorageService` interface với 4 methods (`UploadAsync`, `DeleteAsync`, `GetPublicUrl`, `Exists`). Implementation: `LocalStorageService` với GUID naming, logging. Sẵn sàng swap sang S3/Azure qua DI |
| 3 | **Global Exception Handling** | GĐ 1.3 | ✅ `ExceptionHandlingMiddleware` hoàn chỉnh: RFC 7807 ProblemDetails, pattern matching exception → status code, ẩn stack trace ở production, trả `traceId`. Extension method `UseGlobalExceptionHandler()` |
| 4 | **Pagination** | GĐ 1.6 | ✅ `PagedResult<T>` + `PaginationParams` (max 100/page, default 50). `GetAssets` endpoint hỗ trợ `?page=1&pageSize=50&sortBy=createdAt&sortOrder=desc`. Có `HasNextPage`, `TotalPages` |
| 5 | **File Upload Restrictions** | GĐ 1.5 | ✅ `FileUploadConfig`: max 50MB/file, max 20 files/request, whitelist extensions (image, document, media, archive), MIME type prefix validation. Kestrel `MaxRequestBodySize` = 100MB |
| 6 | **Database Indexing** | GĐ 2.3 | ✅ 9 indexes khai báo trong `OnModelCreating`: `CollectionId`, `ParentFolderId`, composite `(CollectionId, ParentFolderId)`, `ContentType`, `GroupId`, `CreatedAt`, `IsFolder`, `ParentId` (Collection), `Order`, `Type` |
| 7 | **FK Constraints** | GĐ 2.3 | ✅ Asset → Collection (CASCADE delete), Collection → Parent Collection (SET NULL). Khai báo rõ ràng trong Fluent API |
| 8 | **Rate Limiting** | GĐ 3+ | ✅ Hai policy: "Fixed" (100 req/phút), "Upload" (20 req/phút). Trả 429 khi vượt limit. Đăng ký trong pipeline |
| 9 | **Data Validation (DTOs)** | GĐ 1.4 partial | ✅ Data Annotations trên tất cả DTOs: `[Required]`, `[MaxLength]`, `[Range]`. Model entities cũng có `[MaxLength]` constraints. URL validation trên `CreateLinkDto` kiểm tra http/https scheme |
| 10 | **Seed Data** | — | ✅ Sử dụng `HasData()` trong `OnModelCreating` thay vì runtime seeding — đúng pattern cho EF Core |
| 11 | **EF Core Migrations** | GĐ 1.2 | ✅ Chuyển từ `EnsureCreated()` sang `Database.Migrate()`. Migration file `InitialCreate` bao gồm toàn bộ schema + seed data + indexes + FK constraints. `CreatedAt` dùng `HasDefaultValueSql("datetime('now')")` thay vì dynamic `DateTime.UtcNow` trong model. Thư mục `Migrations/` được version control |
| 12 | **Authentication (JWT + Identity)** | GĐ 1.1 | ✅ **MỚI** — ASP.NET Identity + JWT Bearer. `ApplicationUser` kế thừa `IdentityUser` (thêm `DisplayName`, `CreatedAt`). `AuthService` xử lý register/login + JWT generation. `AuthController`: `POST /api/auth/register`, `POST /api/auth/login`. Tất cả controller có `[Authorize]` (trừ Auth + Health). JWT config trong `appsettings.json` |
| 13 | **User Entity + Data Ownership** | GĐ 1.7 | ✅ **MỚI** — `UserId` (nullable string FK → `AspNetUsers`) trên cả `Asset` và `Collection`. Mọi service filter theo `UserId == currentUser`. System collections (seed, `UserId == null`) hiển thị cho tất cả user. Tạo/sửa/xóa chỉ được trên dữ liệu của mình |

#### B) Backend — Logic & An toàn dữ liệu

| # | Hạng mục | Chi tiết |
|---|----------|---------|
| 11 | **File cleanup on delete** | ✅ `DeleteAssetAsync` xóa file vật lý khi xóa asset có `FilePath` bắt đầu `/uploads/`. Không xóa folder/link/color |
| 12 | **Orphan prevention** | ✅ Xóa folder → con chuyển sang thư mục ông (grandparent). Xóa collection → con chuyển thành top-level (`ParentId = null`) |
| 13 | **Batch reorder** | ✅ `ReorderAssetsAsync` fetch tất cả assets một lần bằng `Where(Contains)` rồi update, tránh N+1 |
| 14 | **URL validation** | ✅ `CreateLinkAsync` kiểm tra `Uri.TryCreate` + scheme `http`/`https`, chặn `javascript:` URLs |
| 15 | **CORS cải thiện** | ✅ Từ `AllowAnyOrigin` → config-driven `WithOrigins()` đọc từ `appsettings.json`, chỉ cho phép origins cụ thể |
| 16 | **Server-side Search** | GĐ 2.2 | ✅ **MỚI** — `SearchController` với `GET /api/search?q=&type=&collectionId=&page=&pageSize=`. Tìm kiếm cả Assets (theo tên + tags) và Collections (theo tên + mô tả). Hỗ trợ phân trang |
| 17 | **Health Check** | GĐ 3+ | ✅ **MỚI** — `HealthController` với `GET /api/health`. Kiểm tra database connectivity + storage availability. Trả status `healthy`/`degraded` + environment info + version |

#### C) Frontend — Kiến trúc

| # | Hạng mục | Thuộc giai đoạn | Chi tiết |
|---|----------|-----------------|---------|
| 18 | **Custom Hooks** | GĐ 2.5 | ✅ `useCollections()` (178 dòng): quản lý collections, breadcrumb, folder navigation, CRUD. `useAssets()` (186 dòng): upload, create, move, reorder, select. `App.jsx` giảm từ ~650 → 395 dòng |
| 19 | **API Abstraction Layer** | GĐ 2.5 | ✅ 3 module: `client.js` (axios instance + interceptors + env config), `assetsApi.js` (11 API functions), `collectionsApi.js` (4 API functions), `searchApi.js` (1 API function) |
| 20 | **Error Boundary** | GĐ 2.5 | ✅ `ErrorBoundary.jsx`: Class component, `getDerivedStateFromError`, `componentDidCatch` với logging, UI fallback tiếng Việt, nút "Thử lại" |
| 21 | **Environment-based config** | DevOps | ✅ `VITE_API_URL` và `VITE_STATIC_URL` env vars. Không hardcode URL trong source |
| 22 | **Axios interceptors** | GĐ 2.5 | ✅ Response interceptor log lỗi theo status code, handle no-response và setup errors |
| 23 | **Debounced Search** | Performance | ✅ **MỚI** — Search input 300ms debounce, tránh re-render/filter mỗi keystroke |

#### D) Frontend — UI/UX Components (8 components)

| Component | Dòng | Vai trò | Đặc điểm nổi bật |
|-----------|------|---------|-------------------|
| `CollectionTree` | 97 | Cây sidebar | Recursive rendering, expand/collapse, icons theo type |
| `CollectionBrowser` | 153 | File manager chính | HTML5 Drag & Drop, 3 layout modes, reorder arrows, search filter |
| `AssetDisplayer` | 99 | Gallery / Canvas | Delegate canvas mode → `DraggableAssetCanvas`, fallback images |
| `AssetGrid` | 19 | Grid cơ bản | Auto-fill grid, tags display |
| `ColorBoard` | 85 | Quản lý bảng màu | Grouped columns, hex input, useMemo optimization |
| `DraggableAssetCanvas` | 99 | Canvas kéo thả | Mouse events, boundary clamping, auto-save position |
| `UploadArea` | 22 | Dropzone upload | react-dropzone, drag-active visual |
| `SearchBar` | 15 | Tìm kiếm phụ | Simple input |
| `ErrorBoundary` | 55 | Error catching | Class component, Vietnamese fallback UI |

---

### 9.3 NHỮNG THỨ CHƯA THỰC HIỆN

#### Giai đoạn 1 — ✅ HOÀN THÀNH 100% (7/7)

Tất cả hạng mục GĐ 1 đã được implement. Hệ thống đạt mức Production cơ bản.

#### Giai đoạn 2 — Còn thiếu (1/6 hạng mục)

| # | Hạng mục | Mức ưu tiên | Ghi chú |
|---|----------|-------------|---------|
| 4 | **React Router** | 🟡 MEDIUM | URL không phản ánh trạng thái UI. Không bookmark/share/back-forward được |

#### Giai đoạn 3 — Còn thiếu (4+ hạng mục)

| # | Hạng mục | Mức ưu tiên | Ghi chú |
|---|----------|-------------|---------|
| 5 | **Chuyển sang PostgreSQL** | 🟡 MEDIUM | SQLite giới hạn ~100 concurrent reads, single-writer. Cần cho multi-user |
| 6 | **Docker + docker-compose** | 🟡 MEDIUM | Không có containerization. Deploy thủ công |
| 7 | **CI/CD Pipeline** | 🟡 MEDIUM | Không có automated build/test/deploy |
| 8 | **Structured Logging (Serilog)** | 🟡 MEDIUM | Chỉ có console logger mặc định |
| 9 | **Unit/Integration Tests** | 🟡 MEDIUM | Không có test project nào. Refactor = đánh cược |
| 10 | **FluentValidation** | 🟢 LOW | Data Annotations đang cover cơ bản. FluentValidation cho complex rules |
| 11 | **Thumbnail generation** | 🟢 LOW | Serve original file cho mọi kích thước |
| 12 | **CDN / Caching** | 🟢 LOW | Mọi request file đi qua backend server |

---

### 9.4 MỚI BỔ SUNG (Phiên đánh giá 25/02/2026)

Sau khi đánh giá toàn bộ codebase, các cải thiện sau đã được bổ sung ngay:

| # | Bổ sung | File | Lý do |
|---|---------|------|-------|
| 1 | **Server-side Search API** | `Controllers/SearchController.cs` | Tìm kiếm từ client-side `.includes()` không scale. Endpoint mới hỗ trợ: search by name + tags, filter by type + collectionId, phân trang |
| 2 | **Health Check Endpoint** | `Controllers/HealthController.cs` | Cần cho monitoring, load balancer readiness. Kiểm tra DB + storage, trả version + environment |
| 3 | **Search API Frontend** | `src/api/searchApi.js` | Module gọi search endpoint mới |
| 4 | **Debounce Search** | `src/App.jsx` | Search input trước đó fire mỗi keystroke, gây re-render không cần thiết. Thêm 300ms debounce |
| 5 | **Xóa Dead Code** | `src/components/CollectionTree.jsx` | Loại bỏ `showNewForm`, `newCollectionName`, `handleAddCollection` — khai báo nhưng không bao giờ dùng |
| 6 | **EF Core Migrations** | `Program.cs`, `Data/AppDbContext.cs`, `Migrations/` | Thay `EnsureCreated()` bằng `Database.Migrate()`. Sử dụng `HasDefaultValueSql` cho `CreatedAt`. Seed data dùng static `DateTime`. Migration `InitialCreate` bao gồm toàn bộ schema |
| 7 | **Authentication (JWT + Identity)** | `Models/ApplicationUser.cs`, `Models/AuthDTOs.cs`, `Services/AuthService.cs`, `Controllers/AuthController.cs`, `Program.cs`, `appsettings.json` | ASP.NET Identity + JWT Bearer. Register/Login endpoints. `[Authorize]` trên tất cả controllers (trừ Auth, Health). JWT config đọc từ `appsettings.json` |
| 8 | **User Entity + Data Ownership** | `Models/Asset.cs`, `Models/Collection.cs`, `Data/AppDbContext.cs`, `Services/*.cs`, `Controllers/*.cs` | `UserId` FK trên Asset + Collection. Service layer filter theo user. System collections (seed) hiển thị cho all users. Controllers extract userId từ JWT claims |

---

### 9.5 PHÂN TÍCH CHẤT LƯỢNG CODE HIỆN TẠI

#### Backend — Điểm mạnh

| Khía cạnh | Điểm | Ghi chú |
|-----------|------|---------|
| Tách lớp (Separation) | 9/10 | Controller → Service → DbContext. Storage abstracted. Clean DI registration |
| Data integrity | 8/10 | FK constraints, cascade/set null, orphan prevention, file cleanup |
| Validation | 7/10 | Data annotations + manual checks trong services. URL scheme validation. Chưa dùng FluentValidation |
| Error handling | 8/10 | Global middleware RFC 7807, exception mapping, traceId. Ẩn details ở production |
| Performance | 7/10 | Pagination, batch reorder, index coverage tốt. Chưa có caching |
| Security | 7/10 | Rate limiting + CORS config + JWT Authentication + Data Ownership. [Authorize] trên tất cả controllers. UserId scoping trên mọi query |

#### Frontend — Điểm mạnh

| Khía cạnh | Điểm | Ghi chú |
|-----------|------|---------|
| Code organization | 8/10 | Hooks + API layer tách rõ. App.jsx gọn (<400 dòng) |
| Component design | 7/10 | 9 components, mỗi component đơn trách nhiệm. Props interface rõ ràng |
| Error handling | 6/10 | ErrorBoundary ở root, try/catch trong hooks. Image fallbacks. Chưa có loading skeleton |
| UX | 7/10 | Dark theme nhất quán, 3 layout modes, breadcrumb, drag-and-drop. Chưa responsive |
| Performance | 6/10 | Debounced search, useMemo/useCallback. Chưa có React.memo, virtualization |
| Accessibility | 3/10 | Không aria-*, không keyboard nav trên tree/grid, không focus management |

---

### 9.6 THỐNG KÊ DỰ ÁN

> **Cập nhật:** Sau Phase 1 refactor (8 tasks hoàn thành)

#### Quy mô code — Backend

| Thành phần | Files | Dòng code (chính xác) |
|------------|-------|----------------------|
| Controllers | 9 | 485 |
| Services (interfaces + impl) | 22 (11+11) | 1,674 |
| Models / DTOs | 11 | 703 |
| Extensions | 1 | 182 |
| Data (AppDbContext) | 1 | 203 |
| Middleware | 1 | 71 |
| Hubs (SignalR) | 1 | 52 |
| Program.cs | 1 | 97 |
| **Tổng Backend** | **~47 files** | **~3,467** |

#### Quy mô code — Frontend (chưa refactor)

| Thành phần | Files | Dòng code (ước tính) |
|------------|-------|---------------------|
| Frontend Components | 9 (.jsx) | ~644 |
| Frontend Hooks | 2 | ~364 |
| Frontend API | 4 | ~115 |
| Frontend App | 1 | ~395 |
| Frontend Styles | 10+ (.css) | ~800+ |
| **Tổng Frontend** | **~26+ files** | **~2,318+** |

| | | |
|---|---|---|
| **TỔNG DỰ ÁN** | **~73+ files** | **~5,785+** |

#### API Endpoints

| Controller | Endpoints | Methods |
|------------|-----------|---------|
| Assets | 12 | GET(2), POST(6), PUT(2), DELETE(1), PATCH(1) |
| Collections | 5 | GET(2), POST(1), PUT(1), DELETE(1) |
| Auth | 2 | POST(2) |
| Tags | 5 | GET(2), POST(1), PUT(1), DELETE(1) |
| Permissions | 3 | GET(1), POST(1), DELETE(1) |
| SmartCollections | 1 | POST(1) |
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

---

### 9.7 ĐỀ XUẤT BƯỚC TIẾP THEO (Ưu tiên cao → thấp)

| Ưu tiên | Việc cần làm | Độ khó | Ảnh hưởng |
|---------|--------------|--------|-----------|
| 🔴 1 | **React Router** — `/collections/:id`, `/search?q=`, deep linking | Medium | URL shareable, browser back/forward |
| 🟡 2 | **Loading Skeleton / Empty States** | Low | UX mượt hơn, professional hơn |
| 🟡 3 | **Responsive breakpoints** | Medium | Mobile/tablet support |
| 🟡 4 | **Unit Tests** — xUnit + Moq cho services, Jest cho hooks | Medium | Refactor an toàn |
| 🟢 5 | **Docker + docker-compose** | Low | Deploy nhất quán |
| 🟢 6 | **Serilog structured logging** | Low | Log searchable, aggregatable |
| 🟢 7 | **Thumbnail generation** | Medium | Giảm bandwidth, tải nhanh hơn |
| 🟢 8 | **Frontend Login/Register page** | Medium | Hoàn thiện auth flow phía client |

---

### 9.8 KẾT LUẬN

Dự án Visual Asset Hub đã trải qua **Phase 1 OOP Refactoring** hoàn chỉnh (8 tasks). Backend hiện có:
- **TPH Inheritance** (Asset → 5 subtypes) với virtual behavior properties
- **Factory Pattern** (AssetFactory) cho type-safe creation
- **Rich Domain Model** (domain methods trên 4 entities)
- **Type-safe Enums** với EF Core value conversions
- **Navigation Properties** cho quan hệ parent-child
- **Base Controller** giảm duplication
- **Extension Methods** tổ chức DI registration (Program.cs: 255→97 dòng)

**Phase 2 (Service Layer refactoring)** sẽ tách `AssetService` thành các service nhỏ hơn, áp dụng Strategy pattern cho Smart Collections, và extract reusable helpers.

Frontend chưa được refactor — sẽ là Phase 3.
