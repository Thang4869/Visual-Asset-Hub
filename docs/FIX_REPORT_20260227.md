# Visual Asset Hub — Báo cáo Thay đổi & Sửa lỗi

> **Cập nhật lần cuối:** 28/02/2026 — Session #2

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
### Pending Model Changes — Migration SyncModelChanges
**Vấn đề:** EF Core phát hiện model có thay đổi chưa được ghi vào migration → ứng dụng crash khi khởi động với lỗi `PendingModelChangesWarning`.  
**Nguyên nhân:** Cột `ParentFolderId` trong bảng `Assets` đã tồn tại nhưng thiếu **Foreign Key constraint** trỏ về chính bảng `Assets` (self-referencing FK). Model code đã khai báo relationship nhưng migration trước đó chưa tạo FK tương ứng.  
**Fix:** Tạo migration `20260227165804_SyncModelChanges` bổ sung:
```
FK_Assets_Assets_ParentFolderId (onDelete: Restrict)
```
Đây là self-referencing foreign key cho phép asset có cấu trúc thư mục cha-con (folder hierarchy).

### JSON Enum Serialization — Sửa lỗi hiển thị Colors/Images/Links
**Vấn đề:** Backend trả enum `CollectionType` và `AssetContentType` dưới dạng **số nguyên** (0, 1, 2, 3). Frontend so sánh bằng **string** (`'color'`, `'image'`, `'color-group'`). Kết quả:
- ColorBoard không bao giờ render (Colors collection)
- Ảnh không hiển thị preview trong CollectionBrowser
- Color swatch không render trong grid view
- Sidebar icons (🖼️🔗🎨📁) không hiển thị  
**Fix:** Thêm `JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower)` vào `Program.cs` → `AddControllers().AddJsonOptions(...)`. Giờ enum serialize thành lowercase kebab-case strings khớp frontend:  
`CollectionType.Color` → `"color"`, `AssetContentType.ColorGroup` → `"color-group"`

### Color Code Auto-Prepend `#`
**Vấn đề:** User nhập `6367FF` → lưu thiếu `#` → CSS `backgroundColor` không nhận diện → swatch hiển thị trống.  
**Fix:**
- Backend (`AssetService.CreateColorAsync`): Auto-prepend `#` nếu input match regex `^[0-9A-Fa-f]{3,8}$`
- Frontend (`ColorBoard.jsx`): Cùng logic auto-prepend trước khi gửi API

### Thêm chức năng Xóa từng item
**Vấn đề:** Không có UI để xóa thư mục con, màu sắc, nhóm màu, link, hay tệp riêng lẻ.  
**Fix:** Thêm nút xóa (✕) hiện khi hover cho tất cả các loại item:
- `useAssets.js`: Thêm `handleDeleteAsset()` — gọi `DELETE /api/assets/{id}` với confirm dialog
- `CollectionBrowser.jsx`: Nút ✕ trên mỗi thư mục con và tệp tin (folder, image, link, color)
- `ColorBoard.jsx`: Nút ✕ trên mỗi nhóm màu và từng mẫu màu
- `App.jsx`: Wire `onDeleteAsset={handleDeleteAsset}` cho cả CollectionBrowser và ColorBoard
- CSS: Nút đỏ tròn, opacity 0 → 1 khi hover parent element

**⚠️ GHI CHÚ:** User yêu cầu bỏ nút đỏ ✕, thay bằng Ctrl+click multi-select → thanh bulk delete. Xem mục tiếp theo.

---

## Session #2 — Sửa lỗi toàn diện (28/02/2026)

### Tổng quan vấn đề
User báo 4 lỗi chính:
1. Hình ảnh tải lên nhưng không hiển thị preview
2. Không tạo được group cho màu sắc
3. Link chỉ hiển thị icon 🔗, không có tên và URL clickable
4. Không xóa được màu bằng Ctrl+click

### Nguyên nhân gốc: `AssetFactory` không set `ContentType`

**Phát hiện:** Đây là bug nghiêm trọng nhất, ảnh hưởng đến **tất cả chức năng**.

Lớp `Asset` có property `ContentType` với giá trị mặc định `AssetContentType.File`. EF Core TPH dùng `ContentType` làm discriminator — khi **đọc** từ DB, EF Core tự gán đúng giá trị. Nhưng khi **tạo mới**, `AssetFactory` tạo đúng subtype (`LinkAsset`, `ImageAsset`, v.v.) mà **không set `ContentType`**.

**Hệ quả:**
- API response trả `contentType: "file"` cho tất cả asset mới tạo
- Frontend check `asset.contentType === 'image'` → `false` → không render `<img>`
- `asset.contentType === 'color-group'` → `false` → ColorBoard filter trả rỗng
- `asset.contentType === 'link'` → `false` → Link không hiển thị đúng
- `asset.contentType === 'color'` → `false` → Color swatch không render

**Minh chứng bằng API test:**
```
POST /api/assets/create-link → contentType: "file"   (sai, phải là "link")
POST /api/assets/create-color-group → contentType: "file"   (sai, phải là "color-group")
```

---

### Fix 1: Set `ContentType` trong `AssetFactory` (Backend)

**File:** `VAH.Backend/Models/AssetFactory.cs`

Thêm `ContentType = AssetContentType.X` vào **tất cả 6 factory methods**:

| Method | ContentType được set |
|--------|---------------------|
| `CreateImage()` | `AssetContentType.Image` |
| `CreateFile()` | `AssetContentType.File` |
| `CreateFolder()` | `AssetContentType.Folder` |
| `CreateColor()` | `AssetContentType.Color` |
| `CreateColorGroup()` | `AssetContentType.ColorGroup` |
| `CreateLink()` | `AssetContentType.Link` |

**Kết quả sau fix:**
```
POST /api/assets/create-link → contentType: "link"   ✅
POST /api/assets/create-color-group → contentType: "color-group"   ✅
```

---

### Fix 2: Sửa dữ liệu cũ trong DB (Backend Startup)

**File:** `VAH.Backend/Program.cs`

Các asset tạo trước fix đều có `ContentType = 'file'` trong DB. Thêm SQL sửa dữ liệu chạy khi khởi động (sau migrate):

```sql
-- Image: file trong wwwroot/uploads, thuộc collection type=image
UPDATE Assets SET ContentType = 'image'
WHERE ContentType = 'file' AND FilePath LIKE '/uploads/%'
  AND IsFolder = 0 AND CollectionId IN (SELECT Id FROM Collections WHERE Type = 'image');

-- Link: filePath là URL
UPDATE Assets SET ContentType = 'link'
WHERE ContentType = 'file' AND FilePath LIKE 'http%' AND IsFolder = 0;

-- Color: filePath là hex code, thuộc collection type=color
UPDATE Assets SET ContentType = 'color'
WHERE ContentType = 'file' AND FilePath LIKE '#%'
  AND IsFolder = 0 AND CollectionId IN (SELECT Id FROM Collections WHERE Type = 'color');

-- Color Group: filePath rỗng, thuộc collection type=color
UPDATE Assets SET ContentType = 'color-group'
WHERE ContentType = 'file' AND FilePath = ''
  AND IsFolder = 0 AND CollectionId IN (SELECT Id FROM Collections WHERE Type = 'color');

-- Folder: IsFolder = true
UPDATE Assets SET ContentType = 'folder'
WHERE ContentType = 'file' AND IsFolder = 1;
```

---

### Fix 3: Hiển thị ảnh với Thumbnail Fallback (Frontend)

**File:** `VAH.Frontend/src/components/CollectionBrowser.jsx`

**Trước:** `<img src={staticUrl(asset.filePath)}>`  
**Sau:**
```jsx
<img
  src={staticUrl(asset.thumbnailMd || asset.thumbnailSm || asset.filePath)}
  alt={asset.fileName}
  loading="lazy"
  onError={(e) => {
    // Nếu thumbnail lỗi → thử file gốc
    if (asset.thumbnailMd && e.target.src !== staticUrl(asset.filePath)) {
      e.target.src = staticUrl(asset.filePath);
      return;
    }
    e.target.style.display = 'none';
    e.target.nextElementSibling.style.display = 'flex';
  }}
/>
```

**Cải thiện:**
- Dùng `thumbnailMd` (400px WebP) → nhẹ hơn file gốc (JPEG/BMP)
- Fallback chain: `thumbnailMd` → `thumbnailSm` → `filePath` gốc
- `loading="lazy"` → tải ảnh khi scroll vào viewport
- `onError` graceful: nếu thumbnail lỗi, thử file gốc; nếu vẫn lỗi, hiện icon 📄

---

### Fix 4: Link hiển thị tên + URL clickable (Frontend)

**File:** `VAH.Frontend/src/components/CollectionBrowser.jsx`

**Trước:** Link chỉ hiện icon 🔗, click card chỉ select.  
**Sau:**
- Icon 🔗 là `<a>` tag clickable → mở link trong tab mới
- Hiển thị URL dưới tên file, cũng clickable
- `e.stopPropagation()` để click link không trigger select

```jsx
{asset.contentType === 'link' && (
  <a className="file-icon link-icon"
     href={asset.filePath} target="_blank" rel="noopener noreferrer"
     onClick={(e) => e.stopPropagation()}>
    🔗
  </a>
)}
<div className="item-name">{asset.fileName}</div>
{asset.contentType === 'link' && asset.filePath && (
  <a className="item-link-url"
     href={asset.filePath} target="_blank" rel="noopener noreferrer"
     onClick={(e) => e.stopPropagation()}>
    {asset.filePath}
  </a>
)}
```

**CSS mới** (`CollectionBrowser.css`):
```css
.item-link-url {
  font-size: 0.72rem;
  color: var(--accent);
  text-decoration: none;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  width: 100%;
  opacity: 0.8;
}
.item-link-url:hover { opacity: 1; text-decoration: underline; }
```

---

### Fix 5: Bỏ nút đỏ ✕, thêm Ctrl+click cho ColorBoard (Frontend)

**Yêu cầu user:** Không muốn nút xóa riêng lẻ (nút đỏ ✕). Muốn dùng Ctrl+click multi-select → thanh bulk "Xóa" ở phía trên.

**Xóa bỏ:**
- `CollectionBrowser.jsx`: Bỏ prop `onDeleteAsset`, bỏ tất cả `<button className="item-delete-btn">✕</button>`
- `ColorBoard.jsx`: Bỏ prop `onDeleteAsset`, bỏ tất cả `<button className="group-delete-btn">✕</button>` và `<button className="color-delete-btn">✕</button>`
- `App.jsx`: Bỏ `onDeleteAsset={handleDeleteAsset}` khỏi cả 2 component
- `CollectionBrowser.css`: Xóa `.item-delete-btn` styles
- `ColorBoard.css`: Xóa `.group-delete-btn`, `.color-delete-btn` styles

**Thêm mới — Ctrl+click multi-select cho ColorBoard:**

`ColorBoard.jsx`:
```jsx
// Props mới
const ColorBoard = ({ items, onCreateColor, onCreateGroup,
                      onSelectAsset, selectedAssetIds = new Set() }) => {
  // ...
  // Color item có multi-select
  <div className={`color-item ${selectedAssetIds.has(color.id) ? 'multi-selected' : ''}`}
       onClick={(e) => onSelectAsset && onSelectAsset(color.id, e)}>
  // Group header có multi-select
  <div className="group-header"
       onClick={(e) => group.id !== null && onSelectAsset(group.id, e)}>
```

`App.jsx`:
```jsx
<ColorBoard
  items={collectionItems.items}
  onCreateColor={handleCreateColor}
  onCreateGroup={handleCreateColorGroup}
  onSelectAsset={toggleSelectAsset}         // MỚI
  selectedAssetIds={selectedAssetIds}        // MỚI
/>
```

`ColorBoard.css` — styles multi-select:
```css
.color-group-column.multi-selected {
  border-color: var(--accent);
  box-shadow: 0 0 0 2px var(--accent);
}
.color-item.multi-selected {
  border-color: var(--accent);
  background-color: rgba(33, 150, 243, 0.2);
  box-shadow: 0 0 0 2px var(--accent);
}
.color-item.multi-selected::after {
  content: '✓'; /* Checkmark badge */
}
```

**Cách dùng:** Ctrl+click chọn nhiều màu/nhóm → thanh bulk actions hiện → bấm "Xóa" để xóa hàng loạt.

---

### Fix 6: Sửa label nút "Tải xuống" → "Liên kết mới" (Frontend)

**File:** `VAH.Frontend/src/App.jsx`

Toolbar hiển thị nút "Tải xuống" (Download) nhưng chức năng thực tế là tạo link mới. Sửa label và icon cho đúng ngữ nghĩa:

**Trước:** `🔗 Tải xuống` (icon download)  
**Sau:** `🔗 Liên kết mới` (icon link chain)

---

### Tóm tắt files đã chỉnh sửa

| File | Thay đổi |
|------|----------|
| `VAH.Backend/Models/AssetFactory.cs` | Set `ContentType` cho tất cả 6 factory methods |
| `VAH.Backend/Program.cs` | Thêm SQL fix dữ liệu cũ khi startup |
| `VAH.Frontend/src/components/CollectionBrowser.jsx` | Bỏ nút ✕, thumbnail fallback, link URL clickable |
| `VAH.Frontend/src/components/CollectionBrowser.css` | Bỏ `.item-delete-btn`, thêm `.item-link-url` |
| `VAH.Frontend/src/components/ColorBoard.jsx` | Bỏ nút ✕, thêm Ctrl+click multi-select |
| `VAH.Frontend/src/components/ColorBoard.css` | Bỏ delete btn styles, thêm `.multi-selected` |
| `VAH.Frontend/src/App.jsx` | Bỏ `onDeleteAsset` props, thêm `onSelectAsset`/`selectedAssetIds` cho ColorBoard, sửa label "Liên kết mới" |