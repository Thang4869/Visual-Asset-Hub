# Visual Asset Hub — Báo cáo Thay đổi & Sửa lỗi

> **Cập nhật lần cuối:** 27/02/2026

---

## Tổng quan 4 Phases phát triển

| Phase | Tên | Trạng thái | Items |
|-------|-----|-----------|-------|
| Phase 1 | Backend Foundation | ✅ Hoàn thành | 5/5 |
| Phase 2 | Frontend Core | ✅ Hoàn thành | 6/6 |
| Phase 3 | Advanced Features | ✅ Hoàn thành | 8/8 |
| Phase 4 | Enhancement & Polish | ✅ Hoàn thành | 7/7 |
| **Tổng** | | **100%** | **26/26** |

---

## Phase 1 — Backend Foundation

### 1.1 ASP.NET Core 9 + EF Core 9 Setup
- Khởi tạo project dùng minimal API pattern
- Cấu hình SQLite (Development) / PostgreSQL (Production)
- Serilog structured logging: Console + RollingFile
- CORS policy cho frontend origins
- ExceptionHandlingMiddleware: bắt tất cả exceptions → JSON response

### 1.2 Authentication (JWT + Identity)
- ASP.NET Identity cho user management
- JWT Bearer authentication (24h expiry)
- Endpoints: Register, Login, GetProfile, ChangePassword
- Cookie fallback disabled → thuần JWT
- Roles claim tự động include trong token

### 1.3 Asset Management
- CRUD endpoints cho assets (GET list/detail, POST create, PUT update, DELETE)
- File upload: multipart/form-data, lưu vào `wwwroot/uploads/`
- Tên file: `{GUID}{extension}` tránh conflict
- Pagination: `PagedResult<T>` + `PaginationParams` (default page=1, size=20)
- SortOrder field cho manual ordering

### 1.4 Collection Management
- CRUD cho collections
- Hierarchical: parent-child (ParentId nullable FK)
- 3 default collections tự động seed: Images, Links, Colors
- Collection types: default, image, link, color

### 1.5 Thumbnail Generation
- ImageSharp pipeline: Resize (max dimension) → Encode WebP (quality 80)
- 3 sizes: sm (150px), md (400px), lg (800px)
- Storage: `wwwroot/uploads/thumbs/{size}_{id}.webp`
- Auto-generate on upload, serve qua static files
- Supported: jpg, jpeg, png, gif, bmp, webp, tiff

---

## Phase 2 — Frontend Core

### 2.1 React + Vite SPA
- Vite 7.3 với HMR hot reload
- React 19.2 + React Router v7.13
- Dark Navy theme (CSS custom properties)
- 3-panel layout: sidebar | main grid | detail panel

### 2.2 Authentication UI
- LoginPage: đăng nhập / đăng ký (toggle mode)
- Token lưu localStorage (`vah_token`)
- Auto-redirect khi chưa đăng nhập
- useAuth hook: login, register, logout, getProfile

### 2.3 Asset Grid & Upload
- AssetGrid: responsive grid + list view toggle
- Drag-and-drop upload (react-dropzone)
- Multi-file upload (tối đa 20 files/lần)
- Progress indicator per file
- Asset types: file, link, color — hiển thị khác nhau

### 2.4 Collection Browser
- CollectionTree: sidebar recursive tree
- Expand/collapse animating
- Active collection highlight
- Right-click menu hoặc hover actions (rename, delete, add child)

### 2.5 Search
- SearchBar: full-text search tên/filename/tags
- Kết quả real-time (debounced 300ms)
- Search result grid reuse AssetGrid component

### 2.6 Detail Panel
- AssetDisplayer: right-side detail panel
- Image preview (dùng thumbnail md/lg)
- Metadata: tên, type, kích thước, ngày tạo/cập nhật
- Tags editor: thêm/xóa tags
- Download button (original file)

---

## Phase 3 — Advanced Features

### 3.1 Tags System (Many-to-Many)
**Backend:**
- Tag entity: Id, Name, UserId, CreatedAt
- AssetTag junction: AssetId + TagId (composite PK)
- TagsController: CRUD + batch add/remove + migrate legacy tags
- Unique index: `IX_Tags_Name_UserId` (lowercase normalized)
- Dedup logic: trim + toLower trước khi insert

**Frontend:**
- TagManager component trong detail panel
- Autocomplete suggest existing tags
- Click tag → filter assets by tag
- Visual chips với "✕" remove button

### 3.2 Smart Collections (Virtual)
**Backend:**
- SmartCollectionService: định nghĩa rules → query assets
- 8 built-in types: recent_7d, recent_30d, all_images, all_links, all_colors, untagged, with_thumbnails
- Mỗi user tag → 1 smart collection `tag:{tagName}`
- SmartCollectionsController: GET list, GET /{id}/assets

**Frontend:**
- CollectionTree tích hợp smart collections với icon ⚡
- Phân biệt visually: smart collections = virtual, không xóa/rename được

### 3.3 SignalR Real-time
**Backend:**
- `AssetHub` inherits `Hub`
- User-specific groups: `user_{userId}`
- Broadcasts: AssetUploaded, AssetUpdated, AssetDeleted, CollectionUpdated
- JWT auth qua query string `?access_token=`

**Frontend:**
- useSignalR hook: connect, subscribe, auto-reconnect
- Khi nhận event → invalidate query cache → re-fetch
- Multi-tab support: tất cả tabs cùng user đồng bộ

### 3.4 Bulk Operations
- Multi-select: Ctrl+click (toggle), Shift+click (range)
- BulkActionsBar: Select all, Clear, Delete selected, Move selected
- Backend: POST `/api/Assets/bulk-delete`, POST `/api/Assets/bulk-move`
- Xóa hàng loạt: file vật lý + thumbnails + DB records

### 3.5 Drag-and-Drop Canvas
- DraggableAssetCanvas component
- Free positioning trên infinite canvas
- Persist position per asset (X, Y coordinates)
- Zoom in/out support
- Toggle giữa Grid view ↔ Canvas view

### 3.6 Link Assets
- AddLinkDialog: nhập URL → lưu dưới dạng asset
- Preview: fetch title từ URL nếu có
- Icon link khác với file/color assets

### 3.7 Color Board
- ColorBoard component cho color collections
- Color picker: chọn màu → lưu dưới dạng asset (hex code)
- Visual grid của color swatches
- Copy hex code khi click

### 3.8 Configuration & File Validation
- FileUploadConfig: MaxFileSize (50MB), MaxFileCount (20), AllowedExtensions (27 types), AllowedMimeTypePrefixes (13 prefixes)
- Validation: cả frontend + backend
- Kestrel body limit: 100MB

---

## Phase 4 — Enhancement & Polish

### 4.1 Undo/Redo System
- UndoRedoManager: in-memory command history
- useUndoRedo hook: `undo()`, `redo()`, `canUndo`, `canRedo`
- Max 50 entries trong history stack
- Keyboard shortcuts: Ctrl+Z (undo), Ctrl+Shift+Z (redo)
- Supported actions: create, delete, rename, move, tag changes

### 4.2 Docker Compose Deployment
- 4 services: postgres, redis, backend, frontend
- PostgreSQL 16-alpine: healthcheck, storage volume
- Redis 7-alpine: healthcheck, persistence
- Backend: multi-stage .NET build, non-root user
- Frontend: multi-stage Node → Nginx, non-root user
- Environment variables inject từ compose

### 4.3 Redis Cache Integration
- Cache: collection lists, asset counts, search results
- TTL: 5-30 phút tùy loại data
- Invalidation: tự động khi CUD operations
- Fallback: nếu Redis unavailable → skip cache (graceful degradation)
- Development mode: optional (chỉ khi Redis available)

### 4.4 Serilog Structured Logging
- Console sink: template custom với timestamp + level + context
- File sink: rolling daily, 30-day retention, `logs/vah-{date}.log`
- Request logging: method, path, status, duration (ms)
- Category filtering: suppress EF Core + ASP.NET internals ở default level

### 4.5 Responsive UI Improvements
- Mobile: sidebar collapse → hamburger menu
- Grid: auto-fit columns (min 180px)
- Detail panel: overlay mode trên mobile
- Upload zone: full-width trên small screens
- Touch: swipe gestures cho navigation

### 4.6 RBAC Permission Model
**Backend:**
- CollectionPermission entity: CollectionId, UserId, Role (Owner/Editor/Viewer)
- PermissionService: CheckPermission, Grant, Revoke, GetSharedUsers
- PermissionsController: 5 endpoints
- Auto-grant Owner khi create collection
- Enforcement: check permission trước mỗi collection/asset operation

**Frontend:**
- ShareDialog component: nhập email → chọn role → grant
- Permission indicators trên collection tree
- Conditional UI: ẩn edit/delete cho Viewer
- Shared collections section riêng trong sidebar

---

## Fixes & Bug History

### Auth 401 Fix
**Vấn đề:** Sau đăng nhập, tất cả API calls trả 401  
**Nguyên nhân:** Token không được gửi trong header Authorization  
**Fix:** Axios interceptor tự động attach `Bearer {token}` cho mọi request

### Collection Default Seed
**Vấn đề:** Collections rỗng khi user mới  
**Fix:** Auto-seed 3 default collections (Images, Links, Colors) khi user đầu tiên register

### Thumbnail Memory Leak
**Vấn đề:** ImageSharp streams không dispose  
**Fix:** Wrap trong `using` statements, async dispose

### SignalR CORS
**Vấn đề:** WebSocket blocked bởi CORS  
**Fix:** Thêm `AllowCredentials()` + explicit origins (không dùng wildcard)

### File Delete Orphan
**Vấn đề:** Xóa asset không xóa file vật lý  
**Fix:** DeleteAsync trong StorageService xóa cả original + 3 thumbnails

### Smart Collection Performance
**Vấn đề:** Large dataset chậm khi load smart collections  
**Fix:** Thêm indexes cho CreatedAt, ContentType, UserId + pagination

### Tag Duplicate
**Vấn đề:** Cùng tag tạo nhiều bản  
**Fix:** Normalize (lowercase + trim) + unique index per user
