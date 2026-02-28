# Visual Asset Hub — Hướng dẫn Triển khai & Phát triển

> **Cập nhật:** 28/02/2026

---

## 1. Cài đặt phát triển local

### 1.1 Yêu cầu hệ thống

| Phần mềm | Phiên bản | Kiểm tra |
|-----------|-----------|----------|
| .NET SDK | 9.0+ | `dotnet --version` |
| Node.js | 18+ (khuyến nghị 22+) | `node --version` |
| npm | 9+ | `npm --version` |
| Git | bất kỳ | `git --version` |

> Docker + Docker Compose cần thiết nếu muốn chạy với PostgreSQL + Redis.

### 1.2 Clone & Setup

```bash
git clone https://github.com/Thang4869/Visual-Asset-Hub.git
cd 1A
```

### 1.3 Chạy Backend (SQLite mode)

```bash
cd VAH.Backend
dotnet restore
dotnet run
```

- Backend tại: **http://localhost:5027**
- Swagger UI: **http://localhost:5027/swagger**
- Database SQLite (`vah_database.db`) tự động tạo + migrate lần đầu
- 3 collection mặc định (Images, Links, Colors) được seed

### 1.4 Chạy Frontend

```bash
cd VAH.Frontend
npm install
npm run dev
```

- Frontend tại: **http://localhost:5173**
- Hot reload enabled (Vite HMR)

### 1.5 Sử dụng

1. Mở **http://localhost:5173**
2. **Đăng ký** tài khoản mới (hoặc đăng nhập nếu đã có)
3. Chọn collection ở sidebar → upload file, tạo thư mục, thêm link, quản lý màu sắc
4. Ctrl+click / Shift+click để multi-select → bulk actions bar
5. Chi tiết panel bên phải: xem metadata, quản lý tags
6. Nút "Chia sẻ" → cấp quyền cho user khác bằng email

---

## 2. Chạy bằng Docker Compose (Production-like)

### 2.1 Khởi động

```bash
cd 1A
docker-compose up --build -d
```

### 2.2 Services

| Service | URL | Mô tả |
|---------|-----|-------|
| Frontend | http://localhost:3000 | Nginx + React SPA |
| Backend | http://localhost:5027 | .NET API + SignalR |
| PostgreSQL | localhost:5432 | Database |
| Redis | localhost:6379 | Cache |

### 2.3 Dừng & Xóa

```bash
# Dừng
docker-compose down

# Dừng + xóa volumes (reset database)
docker-compose down -v
```

### 2.4 Xem logs

```bash
docker-compose logs -f backend
docker-compose logs -f frontend
```

---

## 3. Cấu trúc dự án

```
1A/
├── docker-compose.yml          # 4 services: postgres, redis, backend, frontend
├── VAH.sln                     # Visual Studio solution
├── README.md                   # Quick start guide
├── docs/
│   ├── ARCHITECTURE_REVIEW.md  # Kiến trúc + roadmap
│   ├── PROJECT_DOCUMENTATION.md # Tài liệu kỹ thuật chi tiết
│   ├── IMPLEMENTATION_GUIDE.md # Hướng dẫn này
│   ├── FIX_REPORT_20260227.md  # Lịch sử thay đổi
│   ├── OOP_ASSESSMENT.md      # Đánh giá OOP + refactoring progress
│   └── PHASE1_REPORT.md       # Báo cáo Phase 1 refactoring
│
├── VAH.Backend/
│   ├── Program.cs              # Entry point + DI + middleware
│   ├── VAH.Backend.csproj      # NuGet packages
│   ├── Dockerfile              # Multi-stage .NET build
│   ├── appsettings.json        # Configuration
│   ├── appsettings.Development.json
│   ├── Controllers/            # 9 controllers (43 endpoints)
│   │   ├── BaseApiController.cs    # Abstract base (GetUserId())
│   │   ├── AssetsController.cs     # 16 endpoints
│   │   ├── AuthController.cs
│   │   ├── CollectionsController.cs
│   │   ├── HealthController.cs
│   │   ├── SearchController.cs
│   │   ├── TagsController.cs
│   │   ├── SmartCollectionsController.cs
│   │   └── PermissionsController.cs
│   ├── Services/               # 24 files (10 interfaces + 12 impls + 2 helpers)
│   │   ├── IAssetService.cs / AssetService.cs
│   │   ├── IBulkAssetService.cs / BulkAssetService.cs  # NEW: bulk ops
│   │   ├── AssetCleanupHelper.cs                      # NEW: cleanup utility
│   │   ├── ICollectionService.cs / CollectionService.cs
│   │   ├── IAuthService.cs / AuthService.cs
│   │   ├── IStorageService.cs / LocalStorageService.cs
│   │   ├── IThumbnailService.cs / ThumbnailService.cs
│   │   ├── ITagService.cs / TagService.cs
│   │   ├── INotificationService.cs / NotificationService.cs
│   │   ├── ISmartCollectionService.cs / SmartCollectionService.cs
│   │   ├── SmartCollectionFilters.cs  # NEW: ISmartCollectionFilter + 5 strategies
│   │   ├── ISearchService.cs / SearchService.cs
│   │   └── IPermissionService.cs / PermissionService.cs
│   ├── Models/                 # 11 entity classes + DTOs
│   │   ├── Asset.cs            # TPH base class (domain methods, virtual behavior)
│   │   ├── AssetTypes.cs       # TPH subtypes: ImageAsset, LinkAsset, ColorAsset, etc.
│   │   ├── AssetFactory.cs     # Factory pattern (6 Create methods, sets ContentType)
│   │   ├── Enums.cs            # AssetContentType, CollectionType, LayoutType
│   │   ├── Collection.cs       # Collection (domain methods, nav props)
│   │   ├── ApplicationUser.cs  # Identity user
│   │   ├── Tag.cs              # Tag + AssetTag (domain methods)
│   │   ├── CollectionPermission.cs # Permission + Roles + DTOs
│   │   ├── AuthDTOs.cs
│   │   ├── Common.cs           # PagedResult, PaginationParams, FileUploadConfig
│   │   └── DTOs.cs             # Asset/Tag/Bulk DTOs (incl. BulkMoveGroupDto)
│   ├── Data/
│   │   └── AppDbContext.cs     # EF Core context, 5 DbSets, fluent config, 242 lines
│   ├── Hubs/
│   │   └── AssetHub.cs         # SignalR hub
│   ├── Middleware/
│   │   └── ExceptionHandlingMiddleware.cs
│   ├── Migrations/             # 5 migrations
│   └── wwwroot/uploads/        # File storage + thumbnails
│
└── VAH.Frontend/
    ├── package.json            # Dependencies
    ├── vite.config.js          # Vite config
    ├── Dockerfile              # Multi-stage Node + Nginx
    ├── nginx.conf              # SPA fallback + caching
    ├── index.html
    └── src/
        ├── main.jsx            # Entry point
        ├── App.jsx             # Main layout + routes
        ├── App.css             # Dark Navy theme
        ├── api/                # 11 API files (class-based architecture)
        │   ├── BaseApiService.js  # Abstract base class
        │   ├── TokenManager.js    # JWT token singleton
        │   ├── client.js          # Axios instance + interceptors
        │   ├── index.js           # Barrel exports
        │   └── *Api.js            # 7 domain API service classes
        ├── hooks/              # 11 custom hooks
        ├── context/            # State management (AppContext)
        ├── models/             # Domain model classes (Asset, Collection, Tag)
        └── components/         # 14 components
```

---

## 4. Database Migrations

### 4.1 Danh sách migrations

| # | Migration | Mô tả |
|---|-----------|-------|
| 1 | `InitialCreate` | Assets, Collections, Identity tables |
| 2 | `AddThumbnailColumns` | ThumbnailSm, ThumbnailMd, ThumbnailLg |
| 3 | `AddTagSystem` | Tags, AssetTags (M2M junction) |
| 4 | `AddCollectionPermissions` | CollectionPermissions (RBAC) |
| 5 | `SyncModelChanges` | FK_Assets_Assets_ParentFolderId (self-ref FK) |

### 4.2 Tạo migration mới

```bash
cd VAH.Backend
dotnet ef migrations add <MigrationName>
```

### 4.3 Apply migrations

Migrations tự động apply khi khởi động (`db.Database.Migrate()` trong Program.cs).

Hoặc manually:

```bash
dotnet ef database update
```

### 4.4 Đổi sang PostgreSQL

1. Sửa `appsettings.json`:
   ```json
   {
     "DatabaseProvider": "PostgreSQL",
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=vah;Username=vah;Password=vah_secret"
     }
   }
   ```
2. Chạy lại backend — migrations sẽ auto-apply trên PostgreSQL

---

## 5. Tính năng chính — Cách sử dụng

### 5.1 Quản lý Collections

- **Tạo:** Nhấn "+" ở sidebar → nhập tên → chọn type (image/link/color/default)
- **Xóa:** Hover collection → nhấn "✕" → confirm
- **Phân cấp:** Collections hỗ trợ parent-child (tree)
- **Chia sẻ:** Nhấn "Chia sẻ" trong toolbar → nhập email + role

### 5.2 Upload & Quản lý Files

- **Upload:** Kéo thả file vào upload zone, hoặc click để chọn
- **Multi-upload:** Hỗ trợ tối đa 20 file/lần, mỗi file tối đa 50MB
- **Thư mục:** Nhấn "Thư mục mới" → tổ chức file phân cấp
- **Liên kết:** Nhấn "Liên kết mới" → nhập URL → lưu dưới dạng link asset
- **Xóa:** Chọn asset → delete (xóa cả file vật lý + thumbnails)
- **Reorder:** List mode → dùng nút ▲/▼ hoặc POST /api/Assets/reorder

### 5.3 Multi-Select & Bulk Operations

- **Ctrl+Click:** Toggle single selection
- **Shift+Click:** Range select (từ last selected đến click)
- **Click thường:** Single select (clear previous)
- **Bulk bar:** Hiện khi có item selected → Chọn tất cả / Bỏ chọn / Xóa / Di chuyển

### 5.4 Tags (Many-to-Many)

- **Tạo tag:** POST `/api/Tags` hoặc qua details panel "+" button
- **Gắn tag:** PUT `/api/Tags/asset/{id}` (replace all) hoặc POST `.../add`
- **Migration:** POST `/api/Tags/migrate` → convert comma-separated legacy tags sang M2M
- **Dedup:** Tags tự động normalize (lowercase, trim) và dedup per user

### 5.5 Smart Collections

- Tự động tạo virtual collections dựa trên rules
- 8 loại: recent (7d, 30d), all images/links/colors, untagged, with thumbnails
- Mỗi tag cũng tạo 1 smart collection tương ứng
- Hiển thị ở sidebar "Bộ sưu tập thông minh"

### 5.6 Real-time Sync (SignalR)

- Khi user A thay đổi → tự động refresh data cho user A
- Hỗ trợ multi-tab: cả 2 tab sẽ cập nhật đồng thời
- Auto-reconnect khi mất kết nối WebSocket
- JWT authentication qua query string

### 5.7 Undo/Redo

- **Ctrl+Z:** Undo last action
- **Ctrl+Shift+Z:** Redo
- Hỗ trợ tối đa 50 actions trong history
- Command pattern: mỗi action có `execute()` + `undo()`

### 5.8 Permissions (RBAC)

| Role | Read | Write | Manage | Delete |
|------|------|-------|--------|--------|
| **Owner** | ✅ | ✅ | ✅ | ✅ |
| **Editor** | ✅ | ✅ | ❌ | ❌ |
| **Viewer** | ✅ | ❌ | ❌ | ❌ |

- Owner = người tạo collection (tự động)
- Grant/Revoke qua ShareDialog (nhập email)
- Manage = quyền cấp/thu hồi quyền cho người khác

---

## 6. Thumbnails

### 6.1 Kích thước

| Size | Pixels (max dimension) | Dùng cho |
|------|----------------------|---------|
| `sm` | 150px | Grid thumbnails |
| `md` | 400px | Preview panels |
| `lg` | 800px | Full previews |

### 6.2 Format

- Output: **WebP**, quality 80, lossy
- Storage: `wwwroot/uploads/thumbs/{size}_{fileId}.webp`
- Generated on upload automatically
- Nếu ảnh gốc nhỏ hơn target → dùng kích thước gốc

### 6.3 Supported Formats

jpg, jpeg, png, gif, bmp, webp, tiff → WebP thumbnails

---

## 7. Logging (Serilog)

### 7.1 Sinks

| Sink | Output | Mô tả |
|------|--------|-------|
| Console | Structured text | `[HH:mm:ss LEV] SourceContext\n  Message` |
| File | `logs/vah-{date}.log` | Rolling daily, 30-day retention |

### 7.2 HTTP Request Logging

```
HTTP GET /api/Collections responded 200 in 12.345ms
```

- Error → LogEventLevel.Error
- 500+ → LogEventLevel.Error
- >3000ms → LogEventLevel.Warning
- Default → LogEventLevel.Information

### 7.3 Minimum Levels

| Category | Level |
|----------|-------|
| Default | Information |
| Microsoft.AspNetCore | Warning |
| Microsoft.EntityFrameworkCore | Warning |

---

## 8. Troubleshooting

### CORS Error

**Triệu chứng:** Browser console hiện CORS errors  
**Fix:** Kiểm tra `Cors:AllowedOrigins` trong `appsettings.json` có đúng origin của frontend

### 401 Unauthorized

**Triệu chứng:** Mọi API call trả 401  
**Fix:** Đảm bảo đã đăng nhập, token còn hạn (24h). Token lưu ở `localStorage.vah_token`

### SQLite Lock

**Triệu chứng:** Database locked khi multiple write  
**Fix:** Chuyển sang PostgreSQL cho multi-user (sửa `DatabaseProvider` trong appsettings)

### SignalR Connection Failed

**Triệu chứng:** Console log "SignalR connection failed"  
**Fix:** 
1. Kiểm tra CORS config có `AllowCredentials()`
2. Kiểm tra JWT token hợp lệ
3. Backend phải map hub: `app.MapHub<AssetHub>("/hubs/assets")`

### Docker: Backend Cannot Connect to PostgreSQL

**Triệu chứng:** Backend crash loop  
**Fix:** PostgreSQL healthcheck chưa pass. Chờ hoặc kiểm tra `docker-compose logs postgres`

### Upload Failed

**Triệu chứng:** File upload returns error  
**Check:**
1. File size ≤ 50MB
2. Extension trong whitelist (27 types)
3. MIME type trong allowed prefixes (13 prefixes)
4. Kestrel body limit 100MB
