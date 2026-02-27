# Visual Asset Hub — Đánh giá Kiến trúc & Lộ trình Phát triển

> **Cập nhật lần cuối:** 27/02/2026  
> **Phiên bản:** 4.0 — Hoàn thành 4 giai đoạn phát triển (26/26 hạng mục)

---

## 1. Tổng quan hệ thống

Visual Asset Hub (VAH) là ứng dụng web quản lý tài nguyên số (ảnh, link, bảng màu) với giao diện dark theme hiện đại, hỗ trợ kéo thả, tổ chức theo collection phân cấp, tag many-to-many, chia sẻ RBAC và real-time sync.

### Kiến trúc tổng thể

```
┌──────────────────────────────────────────────────────────────────┐
│                            CLIENTS                                │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐            │
│  │  React 19 SPA│  │  Mobile App  │  │  Public API  │            │
│  │  (Vite 7)    │  │  (future)    │  │  (future)    │            │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘            │
└─────────┼─────────────────┼─────────────────┼────────────────────┘
          │ HTTP + WebSocket│                 │
          ▼                 ▼                 ▼
┌──────────────────────────────────────────────────────────────────┐
│                     REVERSE PROXY (Nginx)                         │
│  SPA fallback • Gzip • Cache /assets/ 1 year • Port 80           │
└───────────────────────────┬──────────────────────────────────────┘
                            │
                            ▼
┌──────────────────────────────────────────────────────────────────┐
│                   ASP.NET Core 9.0 Backend                        │
│  ┌────────────────────────────────────────────────────────────┐   │
│  │ Middleware:                                                  │   │
│  │  ExceptionHandler → CORS → Serilog → RateLimiter →          │   │
│  │  StaticFiles → Auth → Controllers + SignalR Hub              │   │
│  ├────────────────────────────────────────────────────────────┤   │
│  │ 8 Controllers (38 endpoints):                               │   │
│  │  Assets(15) • Auth(2) • Collections(5) • Search(1) •        │   │
│  │  Tags(10) • SmartCollections(2) • Permissions(6) • Health(1)│   │
│  ├────────────────────────────────────────────────────────────┤   │
│  │ 9 Services:                                                  │   │
│  │  Asset • Collection • Auth • Storage • Thumbnail •           │   │
│  │  Tag • Notification • SmartCollection • Permission           │   │
│  ├────────────────────────────────────────────────────────────┤   │
│  │ EF Core 9 (SQLite dev / PostgreSQL prod) • 5 DbSets         │   │
│  │ ASP.NET Identity • Auto-Migrate on Startup                   │   │
│  └────────────────────────────────────────────────────────────┘   │
│  Port 5027 • JWT Bearer • SignalR (/hubs/assets)                  │
└──────────┬───────────────────┬──────────────────┬────────────────┘
           │                   │                  │
           ▼                   ▼                  ▼
    ┌─────────────┐    ┌─────────────┐    ┌────────────────┐
    │ PostgreSQL 17│    │  Redis 7    │    │ Local Storage  │
    │ (Docker)     │    │  (Cache)    │    │ wwwroot/uploads│
    │ Port 5432    │    │  Port 6379  │    │ + /thumbs      │
    └─────────────┘    └─────────────┘    └────────────────┘
```

### Tech Stack

| Layer | Công nghệ | Phiên bản |
|-------|-----------|-----------|
| **Runtime** | .NET | 9.0 |
| **Web Framework** | ASP.NET Core | 9.0 |
| **ORM** | Entity Framework Core | 9.x |
| **DB (Dev)** | SQLite | 3.x (embedded) |
| **DB (Prod)** | PostgreSQL | 17 Alpine |
| **Cache** | Redis (StackExchangeRedis) | 7 Alpine |
| **Auth** | ASP.NET Identity + JWT Bearer | 9.x |
| **Real-time** | SignalR | 9.x |
| **Image Processing** | SixLabors.ImageSharp | 3.1.12 |
| **Logging** | Serilog (Console + File) | 10.x |
| **API Docs** | Swashbuckle/Swagger | 10.1.4 |
| **Frontend** | React | 19.2 |
| **Frontend Build** | Vite | 7.3.1 |
| **Routing** | react-router-dom | 7.13 |
| **HTTP Client** | axios | 1.13.5 |
| **File Upload** | react-dropzone | 15.0 |
| **SignalR Client** | @microsoft/signalr | 10.0 |
| **Container** | Docker + docker-compose | Multi-stage |

---

## 2. Kiến trúc Backend

### 2.1 Dependency Injection (Program.cs)

| Đăng ký | Lifetime | Mô tả |
|---------|----------|-------|
| CORS "Frontend" | Config | Origins từ appsettings, AllowCredentials cho SignalR |
| Rate Limiter "Fixed" | Config | 100 req/min, queue 10 |
| Rate Limiter "Upload" | Config | 20 req/min, queue 5 |
| Kestrel MaxRequestBodySize | Config | 100 MB |
| `AppDbContext` | Scoped | SQLite hoặc PostgreSQL theo `DatabaseProvider` config |
| `DatabaseProviderInfo` | Singleton | Record(ProviderName) với `IsPostgreSql` / `IsSqlite` |
| ASP.NET Identity | — | `ApplicationUser` + `IdentityRole` |
| JWT Bearer Auth | — | Validate Issuer/Audience/SigningKey, ClockSkew=Zero, SignalR query string support |
| Redis / MemoryCache | Singleton | Conditional: Redis nếu có connection string |
| `FileUploadConfig` | Singleton | 50MB max, 20 files/request, 27 extensions, 13 MIME prefixes |
| `IStorageService` → `LocalStorageService` | Scoped | Lưu file tại wwwroot/uploads |
| `IAssetService` → `AssetService` | Scoped | Asset business logic |
| `ICollectionService` → `CollectionService` | Scoped | Collection logic + Redis cache |
| `IAuthService` → `AuthService` | Scoped | Register/Login + JWT generation |
| `IThumbnailService` → `ThumbnailService` | Scoped | ImageSharp: sm/md/lg WebP |
| `ITagService` → `TagService` | Scoped | Tag CRUD + M2M |
| `INotificationService` → `NotificationService` | Scoped | SignalR notification wrapper |
| `ISmartCollectionService` → `SmartCollectionService` | Scoped | Dynamic virtual collections |
| `IPermissionService` → `PermissionService` | Scoped | RBAC permission management |
| SignalR | — | Hub + group management |

### 2.2 Middleware Pipeline (thứ tự thực thi)

```
 Request ──►
 1. UseGlobalExceptionHandler()     ← Bắt mọi exception → ProblemDetails JSON
 2. UseCors("Frontend")             ← CORS cho SPA + SignalR
 3. UseSerilogRequestLogging()      ← HTTP request/response logging
 4. UseRateLimiter()                ← Fixed window rate limiting
 5. UseStaticFiles()                ← Serve uploads + thumbnails
 6. UseSwagger()                    ← Dev only
 7. UseAuthentication()             ← JWT Bearer validation
 8. UseAuthorization()              ← Authorization policies
 9. MapControllers()                ← 38 REST API endpoints
10. MapHub<AssetHub>("/hubs/assets") ← SignalR WebSocket hub
 ◄── Response
```

### 2.3 Database Schema

**5 entity tables + ASP.NET Identity tables:**

| Table | Số cột | FK | Indexes |
|-------|--------|----|---------| 
| `Assets` | 18 | CollectionId, UserId, ParentFolderId(self) | 8 indexes |
| `Collections` | 10 | ParentId(self), UserId | 4 indexes |
| `Tags` | 6 | UserId | 3 indexes (incl. unique composite) |
| `AssetTags` | 2 | AssetId, TagId | Composite PK + 1 index |
| `CollectionPermissions` | 6 | UserId, CollectionId | Unique composite + 1 index |

**Tổng: 22 indexes** — tối ưu cho common query patterns.

**Quan hệ giữa bảng:**

```
AspNetUsers ──┬──< Assets (UserId FK, Cascade)
              ├──< Collections (UserId FK, Cascade)
              ├──< Tags (UserId FK, Cascade)
              └──< CollectionPermissions (UserId FK, Cascade)

Collections ──┬──< Assets (CollectionId FK, Cascade)
              ├──< Collections (ParentId self-ref FK, SetNull)
              └──< CollectionPermissions (CollectionId FK, Cascade)

Assets ──< AssetTags ──> Tags  (M2M junction, Cascade both)
Assets ──< Assets (ParentFolderId self-ref, cho folders)
```

**Seed data:** 3 collections mặc định — Images (type=image), Links (type=link), Colors (type=color).

**Dual-provider:** `DatabaseProviderInfo` record cho phép AppDbContext tạo dialect-aware SQL (ví dụ: `datetime('now')` vs `now()` cho default values).

### 2.4 Exception Handling

`ExceptionHandlingMiddleware` map exception → HTTP status:

| Exception | Status | ProblemDetails |
|-----------|--------|----------------|
| `ArgumentException` | 400 | Bad Request |
| `KeyNotFoundException` | 404 | Not Found |
| `UnauthorizedAccessException` | 401 | Unauthorized |
| `InvalidOperationException` | 409 | Conflict |
| Tất cả khác | 500 | Internal Server Error |

Chi tiết exception chỉ hiện ở Development environment.

### 2.5 SignalR Hub

`AssetHub` tại `/hubs/assets`:

- **Auth:** `[Authorize]`, JWT via query string (`?access_token=...`)
- **Groups:** Mỗi user có group `user:{userId}`, join/leave tự động
- **10 event types:** AssetCreated, AssetUpdated, AssetDeleted, AssetsUploaded, AssetsBulkDeleted, AssetsBulkMoved, CollectionCreated, CollectionUpdated, CollectionDeleted, TagsChanged
- **NotificationService:** Gửi notification tới user group, silently catches errors

---

## 3. API Reference (38 Endpoints)

### 3.1 Assets — `api/Assets` [Authorize]

| # | Method | Route | Mô tả |
|---|--------|-------|-------|
| 1 | GET | `/api/Assets` | Danh sách assets phân trang |
| 2 | POST | `/api/Assets` | Tạo asset mới |
| 3 | POST | `/api/Assets/upload` | Upload multi-file (validation: size, ext, MIME) |
| 4 | PUT | `/api/Assets/{id}/position` | Cập nhật vị trí trên canvas |
| 5 | POST | `/api/Assets/create-folder` | Tạo thư mục |
| 6 | POST | `/api/Assets/create-color` | Tạo asset màu sắc |
| 7 | POST | `/api/Assets/create-color-group` | Tạo nhóm màu |
| 8 | POST | `/api/Assets/create-link` | Tạo liên kết (URL validation) |
| 9 | PUT | `/api/Assets/{id}` | Cập nhật asset (partial) |
| 10 | DELETE | `/api/Assets/{id}` | Xóa asset + file vật lý + thumbnails |
| 11 | POST | `/api/Assets/reorder` | Sắp xếp lại thứ tự |
| 12 | GET | `/api/Assets/group/{groupId}` | Assets theo nhóm |
| 13 | POST | `/api/Assets/bulk-delete` | Xóa hàng loạt |
| 14 | POST | `/api/Assets/bulk-move` | Di chuyển hàng loạt |
| 15 | POST | `/api/Assets/bulk-tag` | Gắn/gỡ tag hàng loạt |

### 3.2 Auth — `api/Auth` [RateLimited]

| # | Method | Route | Mô tả |
|---|--------|-------|-------|
| 1 | POST | `/api/Auth/register` | Đăng ký → JWT token + user info |
| 2 | POST | `/api/Auth/login` | Đăng nhập → JWT token + user info |

### 3.3 Collections — `api/Collections` [Authorize]

| # | Method | Route | Mô tả |
|---|--------|-------|-------|
| 1 | GET | `/api/Collections` | Tất cả collections (own + system + shared) |
| 2 | GET | `/api/Collections/{id}/items` | Items + sub-collections (permission-aware) |
| 3 | POST | `/api/Collections` | Tạo collection mới |
| 4 | PUT | `/api/Collections/{id}` | Cập nhật (cần editor+ permission nếu shared) |
| 5 | DELETE | `/api/Collections/{id}` | Xóa (owner only) |

### 3.4 Search — `api/Search` [Authorize]

| # | Method | Route | Mô tả |
|---|--------|-------|-------|
| 1 | GET | `/api/Search?q=&type=&collectionId=&page=&pageSize=` | Tìm kiếm assets + collections |

### 3.5 Tags — `api/Tags` [Authorize]

| # | Method | Route | Mô tả |
|---|--------|-------|-------|
| 1 | GET | `/api/Tags` | Tất cả tags của user |
| 2 | GET | `/api/Tags/{id}` | Chi tiết tag |
| 3 | POST | `/api/Tags` | Tạo tag (dedup normalized) |
| 4 | PUT | `/api/Tags/{id}` | Cập nhật tag |
| 5 | DELETE | `/api/Tags/{id}` | Xóa tag |
| 6 | GET | `/api/Tags/asset/{assetId}` | Tags của 1 asset |
| 7 | PUT | `/api/Tags/asset/{assetId}` | Set toàn bộ tags (replace) |
| 8 | POST | `/api/Tags/asset/{assetId}/add` | Thêm tags |
| 9 | POST | `/api/Tags/asset/{assetId}/remove` | Gỡ tags |
| 10 | POST | `/api/Tags/migrate` | Migrate comma-separated → M2M |

### 3.6 Smart Collections — `api/SmartCollections` [Authorize]

| # | Method | Route | Mô tả |
|---|--------|-------|-------|
| 1 | GET | `/api/SmartCollections` | Danh sách smart collections (8 built-in + per-tag) |
| 2 | GET | `/api/SmartCollections/{id}/items` | Items phân trang |

### 3.7 Permissions — `api/collections/{collectionId}/permissions` [Authorize]

| # | Method | Route | Mô tả |
|---|--------|-------|-------|
| 1 | GET | `.../permissions` | Danh sách quyền (viewer+ access) |
| 2 | POST | `.../permissions` | Cấp quyền (owner only, by email) |
| 3 | PUT | `.../permissions/{permissionId}` | Cập nhật role (owner only) |
| 4 | DELETE | `.../permissions/{permissionId}` | Thu hồi (owner only) |
| 5 | GET | `.../permissions/my-role` | Role hiện tại |
| 6 | GET | `/api/shared-collections` | Collections được chia sẻ cho user |

### 3.8 Health — `api/Health` [No Auth]

| # | Method | Route | Mô tả |
|---|--------|-------|-------|
| 1 | GET | `/api/Health` | DB + Storage checks, env info (200/503) |

---

## 4. Kiến trúc Frontend

### 4.1 Cấu trúc thư mục

```
VAH.Frontend/src/
├── main.jsx                     # StrictMode → ErrorBoundary → BrowserRouter → AuthProvider → App
├── App.jsx                      # AppLayout + Routes (615 lines)
├── App.css                      # Dark Navy theme (24 CSS variables)
│
├── api/                         # 7 API modules
│   ├── client.js                # Axios instance + JWT interceptor + staticUrl
│   ├── assetsApi.js             # 12 functions
│   ├── authApi.js               # register, login
│   ├── collectionsApi.js        # fetchAll, fetchItems, create, delete
│   ├── searchApi.js             # search(params)
│   ├── tagsApi.js               # 10 functions
│   ├── smartCollectionsApi.js   # 2 functions
│   └── permissionsApi.js        # 6 functions
│
├── hooks/                       # 6 custom hooks
│   ├── useAuth.js               # AuthProvider context + login/register/logout
│   ├── useAssets.js             # CRUD + multi-select + bulk operations
│   ├── useCollections.js        # State + URL sync + CRUD
│   ├── useTags.js               # Tag CRUD + asset-tag M2M
│   ├── useSignalR.js            # Real-time connection + events
│   └── useUndoRedo.js           # Command pattern (50 history)
│
└── components/                  # 12 components
    ├── LoginPage.jsx            # Login/Register form
    ├── ErrorBoundary.jsx        # React error boundary
    ├── CollectionTree.jsx       # Sidebar tree navigation
    ├── CollectionBrowser.jsx    # File browser (grid/list/masonry)
    ├── AssetDisplayer.jsx       # Asset gallery + canvas
    ├── AssetGrid.jsx            # Simple card grid
    ├── UploadArea.jsx           # Drag-and-drop upload zone
    ├── ColorBoard.jsx           # Color palette manager
    ├── SearchBar.jsx            # Search input
    ├── ShareDialog.jsx          # RBAC sharing dialog
    └── DraggableAssetCanvas.jsx # Canvas drag-and-drop
```

### 4.2 Routing

| Path | Component | Mô tả |
|------|-----------|-------|
| `/login` | LoginPage | Redirect → `/` nếu đã đăng nhập |
| `/` | AppLayout | Trang chủ hoặc collection browser |
| `/collections/:collectionId` | AppLayout | Xem collection |
| `/collections/:collectionId/folder/:folderId` | AppLayout | Xem thư mục con |
| `*` | Redirect → `/` | Fallback |

### 4.3 State Management

Không sử dụng Redux/Zustand — hoàn toàn React hooks + Context API:

| Hook | Chức năng | Đặc điểm |
|------|-----------|----------|
| `useAuth()` | Auth state, login/register/logout | Context Provider, persist localStorage |
| `useCollections()` | Collection list, selection, CRUD | URL sync qua useParams/useNavigate |
| `useAssets()` | Asset CRUD, multi-select, bulk ops | Ctrl+click toggle, Shift+click range |
| `useTags()` | Tag CRUD, asset-tag management | Auto-fetch on mount |
| `useSignalR()` | Real-time events | Auto-reconnect [0, 2s, 5s, 10s, 30s] |
| `useUndoRedo()` | Command pattern undo/redo | Ctrl+Z / Ctrl+Shift+Z, max 50 history |

### 4.4 Layout

```
┌────────────────────────────────────────────────────────────────┐
│  HEADER (56px)                                                  │
│  Logo • Search Input • Folder/Settings/Notifications/Logout    │
├──────────┬───────────────────────────────────────┬─────────────┤
│ SIDEBAR  │              MAIN AREA                 │  DETAILS    │
│ (240px)  │                                        │  (320px)    │
│          │  Toolbar: Breadcrumbs + Actions + View │             │
│ Tài liệu │  ┌──────────────────────────────────┐  │  Preview    │
│ của tôi  │  │                                    │  │  Metadata   │
│ ├ Images │  │  CollectionBrowser (grid/list/     │  │  Tags       │
│ ├ Links  │  │  masonry) hoặc ColorBoard          │  │  Tag mgmt   │
│ ├ Colors │  │  hoặc SmartCollection view         │  │             │
│ └ Custom │  │                                    │  │             │
│          │  └──────────────────────────────────┘  │             │
│ Smart    │                                        │             │
│ Collections│  Upload Section (react-dropzone)     │             │
│ ├ Gần đây│  Bulk Actions Bar (multi-select)      │             │
│ ├ Images │                                        │             │
│ └ ...    │                                        │             │
├──────────┴───────────────────────────────────────┴─────────────┤
│  ShareDialog (modal overlay) — hiện khi nhấn "Chia sẻ"         │
└────────────────────────────────────────────────────────────────┘
```

---

## 5. Docker & Deployment

### 5.1 Docker Compose — 4 Services

| Service | Image | Port | Depends On | Healthcheck |
|---------|-------|------|------------|-------------|
| `postgres` | postgres:17-alpine | 5432 | — | `pg_isready` |
| `redis` | redis:7-alpine | 6379 | — | `redis-cli ping` |
| `backend` | Build `./VAH.Backend` | 5027 | postgres, redis | `wget /api/Health` |
| `frontend` | Build `./VAH.Frontend` | 3000→80 | backend | — |

### 5.2 Named Volumes

| Volume | Mô tả |
|--------|-------|
| `postgres-data` | PostgreSQL data persistence |
| `redis-data` | Redis data persistence |
| `backend-uploads` | User uploaded files |
| `backend-logs` | Serilog log files |

### 5.3 Docker Build Details

**Backend Dockerfile:**
- Multi-stage: SDK 9.0 (build) → ASP.NET 9.0 (runtime)
- Non-root user `appuser:appgroup`
- Auto-creates `/app/wwwroot/uploads`, `/app/wwwroot/uploads/thumbs`, `/app/logs`

**Frontend Dockerfile:**
- Multi-stage: Node 22 Alpine (build) → Nginx Alpine (serve)
- Build arg: `VITE_API_URL` cho backend URL
- Custom nginx.conf: SPA fallback, gzip, 1-year cache `/assets/`

---

## 6. Bảo mật

| Biện pháp | Chi tiết |
|-----------|---------|
| **JWT Authentication** | 24h expiration, ClockSkew=Zero, HS256 |
| **Password Policy** | Min 6 chars, require digit + lowercase |
| **Data Ownership** | UserId FK trên mọi entity, user chỉ truy cập dữ liệu của mình |
| **RBAC Permissions** | 3 roles (Owner/Editor/Viewer) trên collection, check ở service layer |
| **CORS** | Config-driven origins, AllowCredentials cho SignalR |
| **Rate Limiting** | Fixed window: 100 req/min (general), 20 req/min (upload) |
| **File Validation** | Size (50MB max), extension whitelist (27 types), MIME type check |
| **Exception Privacy** | Exception details chỉ ở Development mode |
| **SignalR Auth** | JWT via query string, user-scoped groups |
| **Docker Security** | Non-root container user, isolated volumes |

---

## 7. Hiệu suất

| Strategy | Chi tiết |
|----------|---------|
| **Redis Cache** | Collection list: 5 min absolute / 2 min sliding |
| **Fallback Cache** | In-memory khi không có Redis |
| **Cache Invalidation** | Auto-invalidate sau mutation |
| **Thumbnail Pre-gen** | sm(150px)/md(400px)/lg(800px) WebP, quality 80 |
| **Nginx Cache** | /assets/ 1-year immutable |
| **Gzip** | css/js/json/svg |
| **DB Indexes** | 22 indexes cho common queries |
| **Pagination** | Server-side, max 100 items/page |

---

## 8. Lộ trình Phát triển — Tổng kết 4 Giai đoạn

### Giai đoạn 1 — Nền tảng Production ✅ 7/7 (100%)

| # | Hạng mục | Files liên quan |
|---|----------|-----------------|
| 1 | Authentication (JWT + Identity) | AuthService, AuthController, useAuth |
| 2 | User Entity + Data Ownership | ApplicationUser, UserId FK |
| 3 | EF Core Migrations | 4 migrations (Initial, Thumbnails, Tags, Permissions) |
| 4 | Exception Handling | ExceptionHandlingMiddleware |
| 5 | Validation | FileUploadConfig, DTO annotations |
| 6 | File Upload Restrictions | AssetService.UploadFilesAsync |
| 7 | Pagination | PagedResult\<T\>, PaginationParams |

### Giai đoạn 2 — Architecture ✅ 6/6 (100%)

| # | Hạng mục | Files liên quan |
|---|----------|-----------------|
| 1 | Service Layer | 9 services với interfaces |
| 2 | Server-side Search | SearchController, SearchResult DTO |
| 3 | Database Indexing | 22 indexes trong AppDbContext |
| 4 | Storage Abstraction | IStorageService → LocalStorageService |
| 5 | Frontend State Refactor | 6 custom hooks |
| 6 | React Router | useCollections URL sync, 4 routes |

### Giai đoạn 3 — Production-Grade ✅ 7/7 (100%)

| # | Hạng mục | Files liên quan |
|---|----------|-----------------|
| 1 | PostgreSQL Dual-Provider | DatabaseProviderInfo, dialect-aware SQL |
| 2 | Docker + docker-compose | 4 services, multi-stage, healthchecks |
| 3 | Redis Cache | CollectionService cache, 5min/2min TTL |
| 4 | Serilog Logging | Console + File sinks, 30-day retention |
| 5 | Health Check | HealthController: DB + Storage |
| 6 | Rate Limiting | Fixed window: 100/min, 20/min upload |
| 7 | Thumbnails | ThumbnailService: sm/md/lg WebP |

### Giai đoạn 4 — Nâng cấp Sản phẩm ✅ 6/6 (100%)

| # | Hạng mục | Files liên quan |
|---|----------|-----------------|
| 1 | Smart Collections | SmartCollectionService, 8 built-in + per-tag |
| 2 | Tag System (M2M) | Tag, AssetTag, TagService, TagsController |
| 3 | Bulk Operations | bulk-delete/move/tag, Ctrl/Shift multi-select |
| 4 | Undo/Redo | useUndoRedo hook, Ctrl+Z/Ctrl+Shift+Z |
| 5 | Real-time Sync | AssetHub, NotificationService, useSignalR |
| 6 | RBAC Permissions | CollectionPermission, PermissionService, ShareDialog |

---

## 9. Tổng kết Metrics

| Metric | Giá trị |
|--------|---------|
| Tổng API endpoints | 38 |
| Backend services | 9 (với interfaces) |
| Frontend hooks | 6 |
| Frontend components | 12 |
| API modules (frontend) | 7 |
| Database tables | 5 entity + Identity |
| Database indexes | 22 |
| Docker services | 4 |
| EF Migrations | 4 |
| Giai đoạn hoàn thành | **4/4 (26/26 — 100%)** |

---

> *Tất cả 4 giai đoạn phát triển đã hoàn thành. Hệ thống sẵn sàng deploy production qua Docker Compose với PostgreSQL, Redis, SignalR real-time, RBAC sharing, và non-root containers.*
