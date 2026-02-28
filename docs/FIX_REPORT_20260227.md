# Visual Asset Hub — Báo cáo Thay đổi & Sửa lỗi

> **Cập nhật lần cuối:** 01/03/2026 — Session #6.8

---

## Tổng quan 4 Phases phát triển

| Phase | Tên | Trạng thái | Items |
|-------|-----|-----------|-------|
| Phase 1 | Backend Foundation | ✅ Hoàn thành | 5/5 |
| Phase 2 | Frontend Core | ✅ Hoàn thành | 6/6 |
| Phase 3 | Advanced Features | ✅ Hoàn thành | 8/8 |
| Phase 4 | Enhancement & Polish | ✅ Hoàn thành | 7/7 |
| **Tổng** | | **100%** | **26/26** |

> **Ghi chú:** Tổng cộng 43 API endpoints (thêm `POST /api/assets/bulk-move-group` trong Session #3).

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
- Backend: multi-stage .NET build, non-root user, 148 lines Program.cs
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

---

## Session #3 — Cải tiến ColorBoard (28/02/2026)

### Tổng quan vấn đề
User báo 3 vấn đề:
1. Khi có nhiều màu trong group → các item tràn ra ngoài cột (đã sửa session trước, overflow CSS)
2. Không có cách copy mã màu — con trỏ luôn là nắm tay (grab) khi hover
3. Drag-drop chỉ di chuyển 1 màu, không di chuyển nhiều màu cùng lúc
4. Không chọn được vị trí chèn khi kéo thả (chỉ append cuối group)

---

### Fix 7: Click-to-copy mã màu (Frontend)

**File:** `VAH.Frontend/src/components/ColorBoard.jsx`

**Trước:** Toàn bộ color item có `cursor: grab`, click vào mã màu không làm gì.  
**Sau:**
- Color item có `cursor: default` bình thường
- Thêm drag handle icon `⠿` riêng biệt ở bên trái, chỉ phần đó có `cursor: grab`
- `.color-code` span có `cursor: pointer` → click gọi `navigator.clipboard.writeText()`
- Sau khi copy hiện `✓ Copied!` bằng màu xanh lá trong 1.5 giây

```jsx
<span className="color-drag-handle" title="Drag to reorder">⠿</span>
<span className="color-swatch" style={{ backgroundColor: color.filePath }} />
<span
  className={`color-code ${copiedId === color.filePath ? 'copied' : ''}`}
  onClick={(e) => handleCopyCode(e, color.filePath)}
  title="Click to copy"
>
  {copiedId === color.filePath ? '✓ Copied!' : color.filePath}
</span>
```

**CSS mới** (`ColorBoard.css`):
```css
.color-drag-handle {
  cursor: grab;
  color: #666;
  font-size: 0.9rem;
  user-select: none;
}
.color-code {
  cursor: pointer;
  transition: background-color 0.15s, color 0.15s;
}
.color-code:hover {
  background-color: rgba(255, 255, 255, 0.1);
}
.color-code.copied {
  color: #4caf50;
  font-weight: 600;
}
```

---

### Fix 8: Drag-drop với vị trí chèn chính xác (Frontend + Backend)

**Vấn đề:** Trước đây khi kéo màu sang group khác, luôn append ở cuối. Không thể sắp xếp lại thứ tự trong cùng group hoặc chèn vào giữa.

**Frontend — `ColorBoard.jsx`:**
- `calcDropTarget()`: Khi drag over group, duyệt tất cả `.color-item` elements, so sánh `e.clientY` với midpoint → xác định `insertBeforeId`
- Hiện **drop indicator line** (gạch xanh ngang + chấm tròn đầu dòng) ở vị trí sẽ chèn
- `dropTarget` state: `{ groupId, insertBeforeId, position }`

```jsx
const calcDropTarget = (e, groupId, groupColors) => {
  const items = e.currentTarget.querySelectorAll('.color-item');
  let insertBeforeId = null;
  for (const item of items) {
    const rect = item.getBoundingClientRect();
    const midY = rect.top + rect.height / 2;
    if (e.clientY < midY) {
      insertBeforeId = parseInt(item.dataset.colorId, 10);
      break;
    }
  }
  setDropTarget({ groupId, insertBeforeId });
};
```

**CSS mới** (`ColorBoard.css`):
```css
.drop-indicator {
  height: 2px;
  background: var(--accent);
  border-radius: 1px;
  box-shadow: 0 0 4px var(--accent);
}
.drop-indicator::before {
  content: '';
  position: absolute;
  left: -4px; top: -3px;
  width: 8px; height: 8px;
  border-radius: 50%;
  background: var(--accent);
}
```

**Backend — Endpoint mới:**
- `POST /api/assets/bulk-move-group`
- DTO mới `BulkMoveGroupDto`: `AssetIds`, `TargetGroupId`, `InsertBeforeId`
- `BulkMoveGroupAsync()` trong `AssetService.cs`:
  1. Set `GroupId` cho tất cả asset di chuyển
  2. Lấy danh sách existing colors trong target group (trừ moved ones)
  3. Nếu `InsertBeforeId` != null → chèn moved assets trước vị trí đó
  4. Nếu null → append cuối
  5. Gán `SortOrder` = index cho toàn bộ final order

---

### Fix 9: Multi-select drag — di chuyển nhiều màu cùng lúc (Frontend)

**Vấn đề:** Ctrl+click cho chọn nhiều màu, nhưng khi kéo chỉ di chuyển 1 màu.

**Fix — `ColorBoard.jsx`:**
- `handleDragStart()`: Nếu item đang kéo nằm trong `selectedAssetIds` → thu thập tất cả selected color IDs
- Truyền array IDs qua `e.dataTransfer.setData('application/json', JSON.stringify(ids))`
- Custom drag image: hiện badge `"N colors"` khi kéo nhiều item
- `handleDrop()`: Parse array IDs → gọi `onMoveColorsToGroup(ids, targetGroupId, insertBeforeId)`

```jsx
const handleDragStart = (e, colorId) => {
  let ids;
  if (selectedAssetIds.has(colorId) && selectedAssetIds.size > 1) {
    ids = Array.from(selectedAssetIds).filter(id => colors.some(c => c.id === id));
  } else {
    ids = [colorId];
  }
  setDragItemIds(ids);
  e.dataTransfer.setData('application/json', JSON.stringify(ids));
  // Custom badge for multi-drag
  if (ids.length > 1) { /* create temporary DOM badge */ }
};
```

**Hook — `useAssets.js`:**
- Đổi `handleMoveColorToGroup` → `handleMoveColorsToGroup`
- Gọi `assetsApi.bulkMoveGroup(colorIds, targetGroupId, insertBeforeId)`
- Clear selection sau khi move thành công

**API — `assetsApi.js`:**
```js
export const bulkMoveGroup = (assetIds, targetGroupId = null, insertBeforeId = null) =>
  apiClient.post(`${ENDPOINT}/bulk-move-group`, { assetIds, targetGroupId, insertBeforeId });
```

---

### Tóm tắt files đã chỉnh sửa (Session #3)

| File | Thay đổi |
|------|----------|
| `VAH.Backend/Models/DTOs.cs` | Thêm `BulkMoveGroupDto` (AssetIds, TargetGroupId, InsertBeforeId) |
| `VAH.Backend/Services/IAssetService.cs` | Thêm `BulkMoveGroupAsync` interface |
| `VAH.Backend/Services/AssetService.cs` | Implement `BulkMoveGroupAsync` — move + reorder in one transaction |
| `VAH.Backend/Controllers/AssetsController.cs` | Thêm endpoint `POST /api/assets/bulk-move-group` |
| `VAH.Frontend/src/api/assetsApi.js` | Thêm `bulkMoveGroup()` API function |
| `VAH.Frontend/src/hooks/useAssets.js` | Đổi `handleMoveColorToGroup` → `handleMoveColorsToGroup` (multi-select + insertBefore) |
| `VAH.Frontend/src/App.jsx` | Wire `onMoveColorsToGroup` prop cho ColorBoard |
| `VAH.Frontend/src/components/ColorBoard.jsx` | Click-to-copy, drag handle riêng, drop indicator, multi-select drag |
| `VAH.Frontend/src/components/ColorBoard.css` | Styles: drag handle, color-code hover/copied, drop indicator line |

---

## Session #4 — Context Menu, Tree View, Clipboard, Shared-Collection Access & UX Polish (28/02/2026)

### Tổng quan

Session lớn nhất tính đến nay — bổ sung nhiều tính năng UX quan trọng và hoàn thiện hệ thống shared-collection access trên backend. Tổng cộng **~4,600 dòng thay đổi** trên 26 files (không kể log file và upload test files).

### Các tính năng mới

#### 1. Context Menu — Right-click toàn ứng dụng
**Files mới:**
- `VAH.Frontend/src/components/ContextMenu.jsx` (81 dòng) — Reusable context menu component
- `VAH.Frontend/src/components/ContextMenu.css` — Dark theme, viewport-aware positioning

**Tính năng:**
- Right-click trên bất kỳ item (asset, folder, group, collection) → hiện menu ngữ cảnh
- Menu items: Ghim, Xem chi tiết, Sao chép mã màu, Sao chép đường dẫn, Sao chép, Cắt, Dán, Đổi tên, Xóa
- Keyboard shortcut labels hiển thị bên phải mỗi menu item
- Auto-close khi click outside, Escape, hoặc scroll
- Viewport boundary detection — tự điều chỉnh vị trí nếu tràn ra ngoài màn hình

**Tích hợp:** `CollectionBrowser.jsx`, `ColorBoard.jsx`, `TreeViewPanel.jsx` — tất cả đều sử dụng shared `ContextMenu` component.

---

#### 2. ConfirmDialog — Thay thế hoàn toàn `window.confirm/prompt/alert`
**Files mới:**
- `VAH.Frontend/src/components/ConfirmDialog.jsx` (133 dòng) — Unified styled dialog
- `VAH.Frontend/src/components/ConfirmDialog.css` — Animated overlay, 3 variants (danger/info/warning)
- `VAH.Frontend/src/context/ConfirmContext.js` (121 dòng) — Promise-based Context Provider

**3 chế độ:**
| Mode | Input | Output | Thay thế |
|------|-------|--------|----------|
| `confirm` | Message + OK/Cancel | `boolean` | `window.confirm()` |
| `prompt` | Message + Input + OK/Cancel | `string \| null` | `window.prompt()` |
| `alert` | Message + OK | `void` | `window.alert()` |

**API sử dụng:**
```js
const { confirm, prompt, alert } = useConfirm();
const ok = await confirm({ message: 'Xóa?', variant: 'danger' });
const name = await prompt({ message: 'Tên:', defaultValue: '...' });
await alert('Hoàn tất!');
```

**Tích hợp:** Tất cả `window.confirm/prompt/alert` trong `useAssets.js`, `useBulkOperations.js`, `useCollections.js`, `AppContext.js` đã được thay thế. `ConfirmProvider` wraps toàn bộ `<Routes>` trong `App.jsx`.

---

#### 3. TreeViewPanel — Panel cấu trúc bên phải
**Files mới:**
- `VAH.Frontend/src/components/TreeViewPanel.jsx` (489 dòng) — Right sidebar tree view
- `VAH.Frontend/src/components/TreeViewPanel.css` — Collapsible panel, indented tree nodes

**Tính năng:**
- Hiển thị cấu trúc phân cấp của collection hiện tại: sub-collections → folders → files
- Expand/collapse từng node
- Nút "Mở rộng tất cả" / "Thu gọn tất cả"
- Click node → navigate đến collection/folder/asset
- Right-click → context menu (Ghim, Sao chép, Cắt, Dán, Đổi tên, Xóa)
- Color collections: hiển thị color groups → colors bên trong
- Collapsible panel (toggle visibility via button)

---

#### 4. Clipboard System — Copy/Cut/Paste cho assets
**State mới trong `AppContext.js`:**
- `clipboard`: `{ item, type, action: 'copy' | 'cut' }` — lưu item đang copy/cut
- Handlers: `handleCopy`, `handleCut`, `handlePaste`

**Logic:**
- **Copy** → Paste = Duplicate asset (gọi `POST /api/assets/{id}/duplicate`)
- **Cut** → Paste = Move asset (gọi `PUT /api/assets/{id}` với `parentFolderId` hoặc `groupId` mới)
- Paste vào folder → set `parentFolderId`
- Paste vào color-group → set `groupId`
- Paste vào area → clear parent (move to root)

---

#### 5. Pin System — Ghim items nhanh
**State mới trong `AppContext.js`:**
- `pinnedItems`: Array `{ item, type }` — lưu các item đã ghim
- Persist qua `localStorage` key `vah_pinned`
- Handler: `handlePinItem` (toggle), `handleNavigateToPinned` (click → navigate)

**Hiển thị:** Pinned items hiện trong `AppSidebar.jsx` — click để navigate trực tiếp đến collection/folder/asset.

---

#### 6. Folder Multi-Select
**State mới:** `selectedFolderIds: Set<number>` — cho phép Ctrl+click chọn nhiều thư mục
- Bulk actions bar hiện tổng items selected (assets + folders)
- Bulk delete xóa cả folders và assets cùng lúc

---

#### 7. Global Clipboard Paste (Ctrl+V images)
**Thêm `useEffect` trong `AppLayout`:**
- Paste từ clipboard hệ thống (ví dụ: screenshot) → tự động upload ảnh
- Tạo file name: `paste-2026-02-28T12-00-00.png`
- Chỉ khi focus không ở input/textarea
- Chỉ khi đã chọn collection

---

#### 8. Shared-Collection Access Control (Backend)
**Thay đổi lớn nhất session này** — hoàn thiện hệ thống shared-collection access trên backend.

**`AssetService.cs` (280 → 421 dòng):**
- Thêm injection `IPermissionService`
- `FindAssetWithAccessAsync(id, userId, minimumRole)` — private helper kiểm tra quyền:
  - Owner → full access
  - Shared-collection permission → check role (`Viewer`, `Editor`)
  - Không có quyền → throw `KeyNotFoundException`
- `ResolveAssetOwnerAsync(collectionId, actingUserId)` — resolve ai sở hữu asset mới:
  - Collection owner → asset thuộc owner (đảm bảo listing đúng)
  - Shared editor → asset thuộc collection owner (không phải editor)
- Tất cả CRUD operations dùng `FindAssetWithAccessAsync` thay vì `a.UserId == userId`
- Upload: shared editor có thể upload vào collection người khác
- Create folder/color/link: cũng kiểm tra shared-collection permission
- `GetAssetsByGroupAsync`: viewer access cho shared assets
- `ReorderAssetsAsync`: editor access cho shared assets

**`BulkAssetService.cs` (149 → 238 dòng):**
- Thêm injection `IPermissionService`
- `FilterByAccessAsync(assets, userId, minimumRole)` — private helper lọc assets theo quyền
- `BulkDeleteAsync`: lọc assets user có editor access
- `BulkMoveAsync`: kiểm tra target collection access
- `BulkMoveGroupAsync`: lọc assets + existing group items theo editor access
- `BulkTagAsync`: lọc assets theo editor access

**`PermissionService.cs`:**
- Thêm injection `IDistributedCache`
- `InvalidateUserCollectionCacheAsync(userId)` — xóa cache khi thay đổi permission
- Gọi invalidation sau mỗi grant/update/revoke permission
- Đảm bảo user thấy thay đổi permission ngay lập tức

**`DuplicateAssetAsync` — Endpoint mới:**
- `POST /api/assets/{id}/duplicate`
- `DuplicateAssetDto`: `TargetFolderId` optional
- Tạo clone với đúng TPH subtype (EF Core discriminator)
- Clone tên: `"{originalName} (bản sao)"`
- Clone thuộc cùng collection, có thể đặt vào folder khác
- Gửi SignalR notification `AssetCreated`

---

#### 9. Breadcrumb Navigation Fix
**`App.jsx`:**
- Fix breadcrumb khi ở trong folder: click collection cuối cùng trong breadcrumb → quay về collection root (thay vì không làm gì)
- Thêm class `current` cho breadcrumb item cuối khi ở root level

---

#### 10. API Bug Fix — permissionsApi.js
**Vấn đề:** `permissionsApi.js` sử dụng full path `/api/collections/.../permissions` thay vì relative path, gây duplicate prefix `/api/api/...`.
**Fix:** Đổi `_permPath()` sang `/collections/...` và `fetchSharedCollections()` sang `/shared-collections`.

#### 11. API Client Error Logging
**`client.js`:** Chỉ log server errors (5xx) vào console. Client errors (4xx) do callers xử lý — giảm noise trong console.

---

### Tóm tắt files đã chỉnh sửa (Session #4)

| File | Thay đổi |
|------|----------|
| **Backend** | |
| `Controllers/AssetsController.cs` | Thêm endpoint `POST api/assets/{id}/duplicate` |
| `Models/DTOs.cs` | Thêm `DuplicateAssetDto` |
| `Services/IAssetService.cs` | Thêm `DuplicateAssetAsync` interface |
| `Services/AssetService.cs` | +141 dòng: `FindAssetWithAccessAsync`, `ResolveAssetOwnerAsync`, `DuplicateAssetAsync`, shared-collection access control cho tất cả CRUD |
| `Services/BulkAssetService.cs` | +89 dòng: `FilterByAccessAsync`, shared-collection access control cho bulk ops |
| `Services/PermissionService.cs` | +16 dòng: cache invalidation khi thay đổi permission |
| **Frontend — Components mới** | |
| `components/ContextMenu.jsx` | 81 dòng — Reusable context menu |
| `components/ContextMenu.css` | Dark theme context menu styles |
| `components/ConfirmDialog.jsx` | 133 dòng — Unified confirm/prompt/alert dialog |
| `components/ConfirmDialog.css` | Animated dialog styles |
| `components/TreeViewPanel.jsx` | 489 dòng — Right sidebar tree view |
| `components/TreeViewPanel.css` | Collapsible tree panel styles |
| `context/ConfirmContext.js` | 121 dòng — Promise-based dialog context |
| **Frontend — Files sửa đổi** | |
| `App.jsx` | +133 dòng: TreeViewPanel, ConfirmProvider, clipboard paste, breadcrumb fix, folder multi-select |
| `App.css` | +57 dòng: tree view, clipboard, confirm styles |
| `context/AppContext.js` | +161 dòng: clipboard, pin, folder selection, rename, delete handlers |
| `hooks/useAssets.js` | Replace `window.confirm/prompt/alert` → `useConfirm()` |
| `hooks/useBulkOperations.js` | Replace `window.confirm` → `useConfirm()` |
| `hooks/useCollections.js` | Replace `window.confirm/prompt` → `useConfirm()` |
| `hooks/useSharePermissions.js` | Minor: propagate confirm context |
| `api/assetsApi.js` | Thêm `duplicateAsset()` method |
| `api/client.js` | Chỉ log 5xx errors |
| `api/permissionsApi.js` | Fix duplicate `/api/api/` prefix |
| `components/AppSidebar.jsx` | Thêm pinned items, add collection button |
| `components/CollectionBrowser.jsx` | Thêm context menu, folder selection, delete/rename handlers |
| `components/CollectionBrowser.css` | +39 dòng: context menu integration, folder selection styles |
| `components/ColorBoard.jsx` | +209 dòng: context menu, group drag-drop, folder/clipboard integration |
| `components/ColorBoard.css` | +96 dòng: context menu, group drag styles |
| `components/ShareDialog.jsx` | Minor: fix shared collection display |

---

## Session #5 — Rename Collection, Global Shortcuts, DTO Refactor & Missing Feature Analysis (28/02/2026)

### Tổng quan

Session #5 tập trung vào **hoàn thiện các tính năng còn thiếu** được xác định qua đánh giá hoàn thiện dự án. Các thay đổi chính:

1. **Rename Collection hoàn chỉnh** — Kết nối frontend handler với backend PUT API
2. **UpdateCollectionDto** — DTO mới thay thế raw `Collection` entity ở endpoint PUT, sửa bug ghi đè giá trị mặc định
3. **Global keyboard shortcuts** — Mở rộng keyboard handler từ chỉ Ctrl+Z → full shortcut suite
4. **Export fetchCollections** — Sidebar tự động refresh sau khi rename

---

### 5.1 Fix Rename Collection (Frontend ↔ Backend)

**Vấn đề:**
- `handleRenameCollection` trong `AppContext.js` chỉ hiển thị alert "Chức năng đổi tên collection đang phát triển" — stub chưa kết nối API
- `collectionsApi.js` thiếu method `update()`
- Backend PUT `/api/collections/{id}` đã có và hoạt động, nhưng frontend không gọi

**Giải pháp:**

| File | Thay đổi |
|------|----------|
| `api/collectionsApi.js` | Thêm `update(id, payload)` method + export `updateCollection` |
| `context/AppContext.js` | Rewrite `handleRenameCollection` → gọi `collectionsApi.updateCollection()`, refresh sidebar + items |
| `hooks/useCollections.js` | Export thêm `fetchCollections` cho context sử dụng |

**Flow hoàn chỉnh:**
```
User right-click → Rename → showPrompt() → nhập tên mới
  → collectionsApi.updateCollection(id, { name }) → PUT /api/collections/{id}
  → Backend: CollectionService.UpdateAsync → ApplyUpdate(dto) → SaveChanges
  → Frontend: refreshItems() + fetchCollections() + setSelectedCollection(updated)
```

---

### 5.2 UpdateCollectionDto (Backend — Sửa bug partial update)

**Vấn đề:**
- Controller PUT endpoint nhận raw `Collection` entity
- Khi frontend chỉ gửi `{ name: "newName" }`, model binding sẽ set các field không gửi thành giá trị mặc định:
  - `Description` → `""` (ghi đè description thật)
  - `Type` → `Default` (có thể sai)
  - `Order` → `0` (ghi đè thứ tự thật)
  - `LayoutType` → `Grid` (có thể sai)
- Đây là bug **data loss** khi partial update

**Giải pháp:**

```csharp
// DTOs.cs — DTO mới với nullable fields
public class UpdateCollectionDto
{
    [MaxLength(255)]  public string? Name { get; set; }
    [MaxLength(2000)] public string? Description { get; set; }
    [MaxLength(20)]   public string? Color { get; set; }
    public CollectionType? Type { get; set; }
    public int? Order { get; set; }
    public LayoutType? LayoutType { get; set; }
}
```

| File | Thay đổi |
|------|----------|
| `Models/DTOs.cs` | Thêm `UpdateCollectionDto` (nullable fields + DataAnnotations) |
| `Models/Collection.cs` | Rewrite `ApplyUpdate(UpdateCollectionDto dto)` — chỉ update non-null fields |
| `Controllers/CollectionsController.cs` | `UpdateCollection(int id, UpdateCollectionDto dto)` thay vì `Collection` |
| `Services/ICollectionService.cs` | Interface: `UpdateAsync(int id, UpdateCollectionDto dto, string userId)` |
| `Services/CollectionService.cs` | UpdateAsync nhận DTO, bỏ ID mismatch check (không cần vì DTO không có Id) |

**Before vs After:**
```csharp
// BEFORE — BUG: ghi đè description, order, layoutType thành default
public void ApplyUpdate(Collection source) {
    Name = source.Name?.Trim() ?? Name;
    Description = source.Description ?? Description; // "" overwrites real data!
    Type = source.Type;                               // Always overwrites!
    Order = source.Order;                             // Always overwrites!
}

// AFTER — FIXED: chỉ update fields thật sự được gửi
public void ApplyUpdate(UpdateCollectionDto dto) {
    if (dto.Name != null) Name = dto.Name.Trim();
    if (dto.Description != null) Description = dto.Description;
    if (dto.Type.HasValue) Type = dto.Type.Value;
    if (dto.Order.HasValue) Order = dto.Order.Value;
}
```

---

### 5.3 Global Keyboard Shortcuts

**Vấn đề:** Keyboard handler chỉ xử lý `Ctrl+Z` (undo) và `Ctrl+Shift+Z` (redo). Các thao tác clipboard, delete, rename chỉ qua context menu — không có phím tắt toàn cục.

**Giải pháp:** Mở rộng `useEffect` keyboard handler trong `AppContext.js`:

| Phím | Hành động | Điều kiện |
|------|-----------|-----------|
| `Ctrl+Z` | Undo | Luôn hoạt động |
| `Ctrl+Shift+Z` | Redo | Luôn hoạt động |
| `Delete` / `Backspace` | Xóa asset đang chọn | `selectedAssetId != null` |
| `F2` | Rename asset đang chọn | `selectedAssetId != null` |
| `Ctrl+C` | Copy asset đang chọn | `selectedAssetId != null` |
| `Ctrl+X` | Cut asset đang chọn | `selectedAssetId != null` |
| `Ctrl+V` | Paste vào folder/collection hiện tại | `clipboard != null` |
| `Ctrl+A` | Chọn tất cả assets | `selectedCollection != null` |

**An toàn:** Handler bỏ qua khi focus đang ở `INPUT`, `TEXTAREA`, `SELECT` — tránh conflict khi user đang nhập liệu.

---

### 5.4 Phân tích hoàn thiện dự án & Items không thực hiện

Qua phân tích chi tiết, các hạng mục sau đã được đánh giá:

#### ✅ Đã thực hiện trong Session #5
| # | Hạng mục | Trạng thái |
|---|----------|-----------|
| 1 | Rename Collection (endpoint + UI) | ✅ Hoàn thành |
| 2 | UpdateCollectionDto (fix partial update bug) | ✅ Hoàn thành |
| 3 | Global keyboard shortcuts (8 shortcuts) | ✅ Hoàn thành |
| 4 | DTO Validation | ✅ Đã có sẵn (DataAnnotations trên tất cả DTOs) |

#### ⏳ Deferred — Cần thêm thời gian hoặc có blocker
| # | Hạng mục | Lý do defer |
|---|----------|-------------|
| 1 | **Unit Tests** | Cần tạo test project mới (`VAH.Backend.Tests`), mock database, mock services. Ước lượng: 4-6h |
| 2 | **Concurrency Control** | SQLite không hỗ trợ `[Timestamp] byte[]` giống SQL Server. Cần dùng `ConcurrencyStamp` (Guid) + migration + exception handling trong tất cả services. Rủi ro cao nếu không có test coverage |
| 3 | **CI/CD Pipeline** | Cần setup GitHub Actions / Azure DevOps. Phụ thuộc vào hosting platform |
| 4 | **HTTPS** | Cần certificate. Docker Compose hiện dùng HTTP. Production cần reverse proxy (nginx/traefik) với TLS |

---

### Tóm tắt files đã chỉnh sửa (Session #5)

| File | Thay đổi |
|------|----------|
| **Backend** | |
| `Models/DTOs.cs` | Thêm `UpdateCollectionDto` (6 nullable fields + validation) |
| `Models/Collection.cs` | Rewrite `ApplyUpdate` nhận `UpdateCollectionDto`, chỉ update non-null fields |
| `Controllers/CollectionsController.cs` | PUT endpoint nhận `UpdateCollectionDto` thay vì raw entity |
| `Services/ICollectionService.cs` | Interface cập nhật `UpdateAsync` signature |
| `Services/CollectionService.cs` | `UpdateAsync` nhận DTO, bỏ ID mismatch check |
| **Frontend** | |
| `api/collectionsApi.js` | Thêm `update(id, payload)` method + named export `updateCollection` |
| `context/AppContext.js` | 1) Import `collectionsApi` 2) Rewrite `handleRenameCollection` gọi API thật 3) Mở rộng keyboard handler: 8 global shortcuts |
| `hooks/useCollections.js` | Export thêm `fetchCollections` cho sidebar refresh |

**Thống kê:**
- **Backend:** 5 files, ~35 dòng thay đổi
- **Frontend:** 3 files, ~90 dòng thay đổi
- **Tổng:** 8 files, ~125 dòng thay đổi
- **Build:** ✅ `dotnet build` — 0 errors, 0 warnings
- **API endpoints:** Không thêm endpoint mới (sửa signature PUT `/api/collections/{id}`)
- **Breaking changes:** PUT `/api/collections/{id}` body đổi từ `Collection` → `UpdateCollectionDto` (nullable fields)

---

## Session #6 — OOP Refactor Phase 1: AssetsController SRP Split + CreateAssetDto + AssetFactory.Duplicate (01/03/2026)

### Tổng quan

Session #6 bắt đầu **đợt OOP refactoring toàn diện** dựa trên kết quả audit 81 source files (~7,300 dòng). Session này tập trung vào **backend controller layer** — điểm vi phạm SRP nghiêm trọng nhất.

### Phân tích kiến trúc trước refactor

**AssetsController.cs (161 dòng, 17 endpoints)** chịu trách nhiệm:

```
┌─ AssetsController (BEFORE) ──────────────────────────────┐
│                                                           │
│  1. Core CRUD: GET list, POST create, PUT update, DELETE  │
│  2. File Upload: POST upload (multipart)                 │
│  3. Position Update: PUT position (canvas)               │
│  4. Specialized Creation: folder, color, color-group,    │
│     link (4 endpoints)                                    │
│  5. Bulk Operations: delete, move, move-group, tag       │
│     (4 endpoints, different service)                      │
│  6. Reorder: POST reorder                                │
│  7. Group Query: GET by group                            │
│  8. Duplicate: POST duplicate                            │
│                                                           │
│  Dependencies: IAssetService + IBulkAssetService          │
│  → Vi phạm SRP: 8 concerns trong 1 controller            │
│  → Vi phạm ISP: inject IBulkAssetService chỉ dùng 4/17  │
└───────────────────────────────────────────────────────────┘
```

**Vấn đề thiết kế khi scale:**
1. **Fat controller** — 161 dòng, 2 service dependencies, 17 action methods
2. **Mixed concerns** — CRUD + bulk + specialized creation + canvas position cùng class
3. **Bulk ops khác performance profile** — bulk-delete có thể xóa 100+ items, cần khác rate-limit
4. **PostAsset nhận raw `Asset` entity** — exposes internal model, client có thể set `UserId`, `CreatedAt`
5. **DuplicateAssetAsync switch on ContentType** — OCP violation, không có trong factory

### 6.1 SRP Split: AssetsController → AssetsController + BulkAssetsController

```
┌─ AssetsController (AFTER) ────────────────────────────────┐
│  SRP: Single-asset lifecycle + specialized creation        │
│  Dependencies: IAssetService only                          │
│  Route: api/assets                                         │
│                                                            │
│  Core CRUD (6):                                            │
│    GET  /                    → Paginated list              │
│    POST /                    → Create (via CreateAssetDto) │
│    POST /upload              → File upload                 │
│    PUT  /{id}                → Update                      │
│    PUT  /{id}/position       → Canvas position             │
│    DELETE /{id}              → Delete                       │
│                                                            │
│  Extended (5):                                             │
│    POST /{id}/duplicate      → Duplicate                   │
│    POST /reorder             → Reorder                     │
│    GET  /group/{groupId}     → Group query                 │
│    POST /create-folder       → Folder                      │
│    POST /create-color        → Color                       │
│    POST /create-color-group  → Color group                 │
│    POST /create-link         → Link                        │
└────────────────────────────────────────────────────────────┘

┌─ BulkAssetsController (NEW) ──────────────────────────────┐
│  SRP: Batch operations only                                │
│  Dependencies: IBulkAssetService only                      │
│  Route: api/assets (same prefix, backward-compat)          │
│                                                            │
│    POST /bulk-delete         → Batch delete                │
│    POST /bulk-move           → Batch move                  │
│    POST /bulk-move-group     → Batch move within group     │
│    POST /bulk-tag            → Batch tag/untag             │
└────────────────────────────────────────────────────────────┘
```

**Tại sao tách BulkAssetsController?**
- **SRP**: Bulk operations = different performance, error semantics (partial success vs all-or-nothing)
- **ISP**: Controller chỉ inject service nó thực sự dùng
- **Rate limiting**: Future: apply stricter rate limits trên bulk endpoints
- **Testing**: Unit test bulk behavior không cần mock IAssetService
- **Backward-compat**: Giữ nguyên route `api/assets/bulk-*` → frontend không đổi

### 6.2 CreateAssetDto — Loại bỏ raw entity exposure

**BEFORE (bảo mật risk):**
```csharp
// Client có thể set UserId, CreatedAt, Id, ContentType — ghi đè server logic
public async Task<ActionResult<Asset>> PostAsset(Asset asset)
```

**AFTER (DTO chỉ expose fields cần thiết):**
```csharp
public class CreateAssetDto
{
    [Required, MaxLength(500)]
    public string FileName { get; set; } = string.Empty;

    [Required, MaxLength(2048)]
    public string FilePath { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CollectionId { get; set; } = 1;

    public int? ParentFolderId { get; set; }
}

public async Task<ActionResult<Asset>> CreateAsset([FromBody] CreateAssetDto dto)
```

**Impact chain:**
- `IAssetService.CreateAssetAsync` signature: `Asset asset` → `CreateAssetDto dto`
- `AssetService.CreateAssetAsync`: Dùng `AssetFactory.FromDto(dto, userId)` thay vì raw entity
- `AssetFactory`: Thêm `FromDto()` static method

### 6.3 AssetFactory.Duplicate() — Loại bỏ OCP violation

**BEFORE (switch + manual field copy trong service):**
```csharp
// AssetService.DuplicateAssetAsync — 25 dòng switch + copy
Asset clone = source.ContentType switch {
    AssetContentType.Image => new ImageAsset(),
    AssetContentType.Link => new LinkAsset(),
    ...
};
clone.FileName = source.FileName + " (bản sao)";
clone.FilePath = source.FilePath;
// ... 15 more fields manually copied
```

**AFTER (factory encapsulates TPH subtype creation + field copy):**
```csharp
// AssetFactory.Duplicate() — factory owns subtype creation logic
public static Asset Duplicate(Asset source, string userId, int? targetFolderId = null)
{
    Asset clone = source.ContentType switch { ... }; // TPH subtype
    // All 15 fields copied in one place — DRY, maintainable
    return clone;
}

// AssetService — 3 dòng thay vì 25
var clone = AssetFactory.Duplicate(source, userId, targetFolderId);
_context.Assets.Add(clone);
await _context.SaveChangesAsync();
```

**Benefit:** Thêm property mới vào Asset chỉ cần update 1 chỗ (factory) thay vì hunt qua services.

---

### Tóm tắt files đã chỉnh sửa (Session #6)

| File | Thay đổi |
|------|----------|
| **Backend — Controllers** | |
| `Controllers/AssetsController.cs` | Refactored: bỏ IBulkAssetService dependency, dùng CreateAssetDto thay raw entity, sắp xếp lại endpoints theo logical groups |
| `Controllers/BulkAssetsController.cs` | **NEW**: 4 bulk endpoints tách từ AssetsController, inject chỉ IBulkAssetService |
| **Backend — Services** | |
| `Services/IAssetService.cs` | `CreateAssetAsync(Asset)` → `CreateAssetAsync(CreateAssetDto)` |
| `Services/AssetService.cs` | 1) CreateAssetAsync dùng `AssetFactory.FromDto()` 2) DuplicateAssetAsync dùng `AssetFactory.Duplicate()` — giảm 22 dòng |
| **Backend — Models** | |
| `Models/DTOs.cs` | Thêm `CreateAssetDto` (4 fields + validation annotations) |
| `Models/AssetFactory.cs` | Thêm `Duplicate()` + `FromDto()` — factory owns tất cả Asset creation logic |

**Thống kê:**
- **Files thay đổi:** 6 (4 sửa + 1 mới + 1 DTO thêm)
- **Lines thay đổi:** ~100 dòng refactored, ~60 dòng mới (BulkAssetsController)
- **Build:** ✅ `dotnet build` — 0 errors, 0 warnings
- **API routes:** Giữ nguyên 100% — frontend không cần thay đổi
- **Breaking changes:** `POST /api/assets` body đổi từ raw `Asset` → `CreateAssetDto`

---

### Session #6.2 — RESTful API Standardization + HTTP Status Code Fix (01/03/2026)

#### Phân tích kiến trúc (8-point Senior Architect Assessment)

Đánh giá toàn diện AssetsController sau Session #6.1:

| # | Tiêu chí | Điểm | Ghi chú |
|---|----------|------|---------|
| 1 | Clean Architecture | 4/10 | Layered OK, dependency rule violated (Domain → DTO) |
| 2 | RESTful compliance | 5.5/10 | Missing GET/{id}, 4 RPC routes, wrong status codes |
| 3 | SRP | 7/10 | Bulk split done, 6 concerns remain (acceptable) |
| 4 | Security | 7/10 | Auth OK, entity exposure risk |
| 5 | Scalability | 4/10 | N+1 queries, no API versioning |
| 6 | Team readiness | 5/10 | Mega DTOs.cs, no tests |
| 7 | Code quality | 7/10 | Factory, DI, async, error handling good |
| 8 | **Overall** | **Portfolio++ approaching Production-lite** | |

#### Vấn đề phát hiện và sửa

**6.2.1 — Thêm GET /api/assets/{id} (thiếu hoàn toàn)**

Service đã có `GetByIdAsync` nhưng controller không expose → `CreatedAtAction` trỏ sai.

```csharp
// GET: api/assets/{id} — NEW
[HttpGet("{id}")]
public async Task<ActionResult<Asset>> GetAssetById(int id)
{
    var asset = await _assetService.GetByIdAsync(id, GetUserId());
    if (asset == null) return NotFound();
    return Ok(asset);
}
```

**6.2.2 — Fix CreatedAtAction: Location header chuẩn RFC 7231**

| Endpoint | Before | After |
|----------|--------|-------|
| `CreateAsset` | `CreatedAtAction(nameof(GetAssets))` → `/api/assets?id=5` ❌ | `CreatedAtAction(nameof(GetAssetById))` → `/api/assets/5` ✅ |
| `CreateFolder` | `Ok(folder)` → 200 ❌ | `CreatedAtAction(nameof(GetAssetById))` → 201 ✅ |
| `CreateColor` | `Ok(color)` → 200 ❌ | `CreatedAtAction(nameof(GetAssetById))` → 201 ✅ |
| `CreateColorGroup` | `Ok(group)` → 200 ❌ | `CreatedAtAction(nameof(GetAssetById))` → 201 ✅ |
| `CreateLink` | `Ok(link)` → 200 ❌ | `CreatedAtAction(nameof(GetAssetById))` → 201 ✅ |
| `DuplicateAsset` | `Ok(clone)` → 200 ❌ | `CreatedAtAction(nameof(GetAssetById))` → 201 ✅ |
| `UploadFiles` | `Ok(list)` → 200 ❌ | `StatusCode(201, list)` → 201 ✅ |
| `ReorderAssets` | `Ok()` → 200 empty ❌ | `NoContent()` → 204 ✅ |

**6.2.3 — RPC → REST noun routes**

| Before (RPC) | After (REST) | Lý do |
|--------------|--------------|-------|
| `POST /create-folder` | `POST /folders` | REST: verb=POST, noun=folders |
| `POST /create-color` | `POST /colors` | REST: verb=POST, noun=colors |
| `POST /create-color-group` | `POST /color-groups` | REST: verb=POST, noun=color-groups |
| `POST /create-link` | `POST /links` | REST: verb=POST, noun=links |

Frontend `assetsApi.js` updated tương ứng.

**6.2.4 — PUT → PATCH cho partial update**

- `PUT /api/assets/{id}` giữ lại (backward compat) + thêm `PATCH /api/assets/{id}` (chuẩn)
- `PUT /api/collections/{id}` giữ lại + thêm `PATCH /api/collections/{id}` (chuẩn)
- `BaseApiService` thêm `_patch()` helper method
- Frontend `assetsApi.js` + `collectionsApi.js` chuyển sang dùng PATCH

#### Files đã chỉnh sửa (Session #6.2)

| File | Thay đổi |
|------|----------|
| `Controllers/AssetsController.cs` | +GET/{id}, fix tất cả CreatedAtAction, thêm PATCH, routes RPC→REST |
| `Controllers/CollectionsController.cs` | Thêm PATCH endpoint (giữ PUT backward compat) |
| `Frontend: api/BaseApiService.js` | Thêm `_patch()` helper |
| `Frontend: api/assetsApi.js` | Routes: `/create-*` → `/folders`, `/colors`, `/color-groups`, `/links`. PUT → PATCH |
| `Frontend: api/collectionsApi.js` | PUT → PATCH |

**Thống kê:**
- **Files thay đổi:** 5 (3 backend + 2 frontend)
- **Build:** ✅ 0 errors, 0 warnings
- **RESTful score:** 5.5/10 → **7.5/10** (GET/{id}, correct status codes, noun routes, PATCH)
- **Breaking changes:** Routes đổi tên (create-folder→folders, etc.) + PATCH thêm mới. PUT vẫn hoạt động (backward compat)

---

### Session #6.3 — Self-Assessment & Quality Fix (01/03/2026)

#### Tự đánh giá (Self-review)

Sau khi hoàn thành Session #6.1 + 6.2, tự audit lại phát hiện **8 vấn đề chưa xử lý**:

| # | Vấn đề | Severity | Đã fix? |
|---|--------|----------|---------|
| 1 | **DRY violation**: `UpdateAsset` + `UpdateAssetPut` body identical | 🔴 HIGH | ✅ `UpdateAssetPut` → delegate to `UpdateAsset()` |
| 2 | **Thiếu `[ProducesResponseType]`**: Swagger docs sai | 🟡 MEDIUM | ✅ Thêm cho tất cả 15 endpoints |
| 3 | **Thiếu XML doc `/// <summary>`**: IDE/Swagger không hiển thị | 🟡 MEDIUM | ✅ Thêm cho tất cả methods |
| 4 | **Inconsistent error pattern**: `GetByIdAsync` trả null, mọi method khác throw | 🟡 MEDIUM | ✅ `GetByIdAsync` → dùng `FindAssetWithAccessAsync` (throw consistent) |
| 5 | **Thiếu `CancellationToken`**: request hủy nhưng service vẫn chạy | 🟡 MEDIUM | ✅ Thêm cho IAssetService (14 methods), IBulkAssetService (4 methods), AssetService, BulkAssetService, cả 2 controllers |
| 6 | Upload endpoint raw params thay DTO | 🟢 LOW | ⬜ Phase 2 |
| 7 | `group/{groupId}` thiếu pagination | 🟢 LOW | ⬜ Phase 2 |
| 8 | Return raw entity thay Response DTO | 🟢 LOW | ⬜ Phase 2 |

#### Thay đổi chi tiết

**1. DRY fix — PUT backward compat:**
```csharp
// BEFORE: 2 identical method bodies
[HttpPut("{id}")]
public async Task<ActionResult<Asset>> UpdateAssetPut(int id, [FromBody] UpdateAssetDto dto) {
    var asset = await _assetService.UpdateAssetAsync(id, dto, GetUserId()); // DUPLICATE
    return Ok(asset);
}

// AFTER: delegate to PATCH method + hide from Swagger
[HttpPut("{id}")]
[ApiExplorerSettings(IgnoreApi = true)]
public Task<ActionResult<Asset>> UpdateAssetPut(int id, ..., CancellationToken ct)
    => UpdateAsset(id, dto, ct);
```

**2. Error pattern consistency — `GetByIdAsync`:**
```csharp
// BEFORE: 2 error patterns
public async Task<Asset?> GetByIdAsync(int id, string userId) {
    if (asset == null) return null;            // Pattern 1: return null
    ...
    return null;                               // Controller phải check null
}

// AFTER: 1 unified pattern
public async Task<Asset> GetByIdAsync(int id, string userId, CancellationToken ct) {
    return await FindAssetWithAccessAsync(id, userId, CollectionRoles.Viewer, ct);
    // → throws KeyNotFoundException → middleware → 404
}
```

**3. CancellationToken propagation:**
- `IAssetService`: 14 methods thêm `CancellationToken ct = default`
- `IBulkAssetService`: 4 methods thêm `CancellationToken ct = default`
- `AssetService`: Tất cả `SaveChangesAsync()` → `SaveChangesAsync(ct)`, `ToListAsync()` → `ToListAsync(ct)`, `CountAsync()` → `CountAsync(ct)`, `FirstOrDefaultAsync()` → `FirstOrDefaultAsync(..., ct)`, upload loop thêm `ct.ThrowIfCancellationRequested()`
- `BulkAssetService`: Same pattern + `ct.ThrowIfCancellationRequested()` trong bulk delete/tag loops

#### Files đã chỉnh sửa (Session #6.3)

| File | Thay đổi |
|------|----------|
| `Controllers/AssetsController.cs` | +`[Produces]`, +`[ProducesResponseType]` all endpoints, +`/// <summary>`, +CancellationToken all methods, DRY fix (PUT delegates to PATCH), removed null check in GetAssetById |
| `Controllers/BulkAssetsController.cs` | +`[Produces]`, +`[ProducesResponseType]`, +`/// <summary>`, +CancellationToken |
| `Services/IAssetService.cs` | +CancellationToken all 14 methods, `GetByIdAsync` return `Asset` (was `Asset?`) |
| `Services/IBulkAssetService.cs` | +CancellationToken all 4 methods |
| `Services/AssetService.cs` | +CancellationToken all methods + private helpers, pass ct to all EF calls, `GetByIdAsync` uses `FindAssetWithAccessAsync` (consistent error pattern), +`ct.ThrowIfCancellationRequested()` in upload loop |
| `Services/BulkAssetService.cs` | +CancellationToken all methods, pass ct to all EF calls, +`ct.ThrowIfCancellationRequested()` in loops |

**Thống kê:**
- **Files thay đổi:** 6 (4 backend services/interfaces + 2 controllers)
- **CancellationToken added:** 18 public methods + 2 private helpers = 20 signatures
- **Build:** ✅ 0 errors, 0 warnings

---

### Session #6.4 — System-wide Quality Standardization (01/03/2026)

#### Scope
Áp dụng **cùng tiêu chuẩn chất lượng** đã thiết lập ở Session #6.3 cho **toàn bộ** controllers + services còn lại:
- `[Produces("application/json")]` + `[ProducesResponseType]` trên tất cả endpoints
- `/// <summary>` XML docs trên tất cả action methods
- `CancellationToken` trên tất cả async methods (interface → implementation → controller)
- DRY fix cho `UpdateCollectionPut` → delegate to `UpdateCollection`
- **`CreateCollectionDto`** — thay thế raw `Collection` entity trong `PostCollection`

#### Thay đổi chi tiết

**1. CreateCollectionDto (NEW):**
```csharp
// BEFORE: Controller nhận raw entity — SRP violation
public async Task<ActionResult<Collection>> PostCollection(Collection collection)

// AFTER: Controller nhận DTO — clean separation
public async Task<ActionResult<Collection>> PostCollection([FromBody] CreateCollectionDto dto, CancellationToken ct)
```
DTO chỉ chứa: Name (required), Description?, ParentId?, Color?, Type?, LayoutType?.
CollectionService.CreateAsync map DTO → entity (set CreatedAt, UserId server-side).

**2. CancellationToken propagation — ALL remaining services:**

| Interface | Methods updated |
|-----------|----------------|
| `ICollectionService` | 6 methods |
| `IAuthService` | 2 methods |
| `ITagService` | 11 methods |
| `ISearchService` | 1 method |
| `ISmartCollectionService` | 2 methods |
| `IPermissionService` | 7 methods |
| `INotificationService` | 1 method |
| **Total** | **30 interface methods** |

**3. Cross-service ct propagation:**
- `AssetService` → `_notifier.NotifyAsync(..., ct)` — 4 calls updated
- `BulkAssetService` → `_notifier.NotifyAsync(..., ct)` — 3 calls updated
- `CollectionService` → `_permissionService.HasPermissionAsync(..., ct)` — 4 calls, `_notifier.NotifyAsync(..., ct)` — 3 calls, `_cache.RemoveAsync/GetStringAsync/SetStringAsync(..., ct)` — all calls
- `PermissionService` → `_cache.RemoveAsync(..., ct)`, `FindAsync([id], ct)` — all EF calls

**4. DRY fix — CollectionsController PUT compat:**
```csharp
[HttpPut("{id}")]
[ApiExplorerSettings(IgnoreApi = true)]
public Task<IActionResult> UpdateCollectionPut(int id, ..., CancellationToken ct)
    => UpdateCollection(id, dto, ct);  // delegate, no duplicate body
```

#### Files đã chỉnh sửa (Session #6.4)

| File | Thay đổi |
|------|----------|
| `Models/DTOs.cs` | +`CreateCollectionDto` (new class) |
| `Services/ICollectionService.cs` | +CancellationToken 6 methods, `CreateAsync(Collection)` → `CreateAsync(CreateCollectionDto)` |
| `Services/IAuthService.cs` | +CancellationToken 2 methods |
| `Services/ITagService.cs` | +CancellationToken 11 methods |
| `Services/ISearchService.cs` | +CancellationToken 1 method |
| `Services/ISmartCollectionService.cs` | +CancellationToken 2 methods |
| `Services/IPermissionService.cs` | +CancellationToken 7 methods |
| `Services/INotificationService.cs` | +CancellationToken 1 method |
| `Services/CollectionService.cs` | +CancellationToken all methods + `InvalidateCacheAsync`, `CreateAsync` mapped from DTO, `FindAsync([id], ct)`, cache calls +ct, permission calls +ct, notify calls +ct |
| `Services/AuthService.cs` | +CancellationToken 2 methods |
| `Services/TagService.cs` | +CancellationToken all 11 methods + `SyncLegacyTagsFieldAsync`, `FindAsync([id], ct)`, all EF calls +ct, +`ct.ThrowIfCancellationRequested()` in loops |
| `Services/SearchService.cs` | +CancellationToken, all `CountAsync(ct)` + `ToListAsync(ct)` |
| `Services/SmartCollectionService.cs` | +CancellationToken all methods + `BuildTagFiltersAsync` + `FindDynamicFilterAsync`, all EF calls +ct, +`ct.ThrowIfCancellationRequested()` in loops |
| `Services/PermissionService.cs` | +CancellationToken all 7 methods + `InvalidateUserCollectionCacheAsync`, `FindAsync([id], ct)`, all EF calls +ct, +`ct.ThrowIfCancellationRequested()` in ListAsync loop |
| `Services/NotificationService.cs` | +CancellationToken, `SendAsync(..., ct)` |
| `Services/AssetService.cs` | `NotifyAsync` calls updated to pass ct (4 calls) |
| `Services/BulkAssetService.cs` | `NotifyAsync` calls updated to pass ct (3 calls) |
| `Controllers/CollectionsController.cs` | +`[Produces]`, +`[ProducesResponseType]`, +XML docs, +CancellationToken, DRY fix (PUT→PATCH delegate), `PostCollection(CreateCollectionDto)` |
| `Controllers/AuthController.cs` | +`[Produces]`, +`[ProducesResponseType]`, +CancellationToken |
| `Controllers/TagsController.cs` | +`[Produces]`, +`[ProducesResponseType]`, +XML docs, +CancellationToken |
| `Controllers/SearchController.cs` | +`[Produces]`, +`[ProducesResponseType]`, +CancellationToken |
| `Controllers/SmartCollectionsController.cs` | +`[Produces]`, +`[ProducesResponseType]`, +XML docs, +CancellationToken |
| `Controllers/PermissionsController.cs` | +`[Produces]`, +`[ProducesResponseType]`, +explicit return types, +CancellationToken |
| `Controllers/HealthController.cs` | +`[Produces]`, +`[ProducesResponseType]`, +CancellationToken, `CanConnectAsync(ct)` |

**Thống kê:**
- **Files thay đổi:** 24 (8 interfaces + 9 implementations + 7 controllers)
- **CancellationToken added:** 30 interface methods + 30 implementation methods + ~35 controller params = ~95 signatures
- **Cross-service ct propagation:** 14 NotifyAsync calls + 8 PermissionService calls + 6 cache calls
- **OOP Roadmap item fixed:** B3 #20 (`PostCollection(Collection)` → `CreateCollectionDto`)
- **Build:** ✅ 0 errors, 0 warnings
| # | Task | Files | Status |
|---|------|-------|--------|
| 1 | Private setters trên Asset properties (enforce qua domain methods) | `Models/Asset.cs` | ⬜ |
| 2 | Private setters trên Collection properties | `Models/Collection.cs` | ⬜ |
| 3 | Private setters trên Tag properties | `Models/Tag.cs` | ⬜ |
| 4 | Private setters trên CollectionPermission | `Models/CollectionPermission.cs` | ⬜ |
| 5 | Remove DTO dependency từ domain models — `Asset.ApplyUpdate(UpdateAssetDto)` → service handles mapping | `Models/Asset.cs`, `Services/AssetService.cs` | ⬜ |
| 6 | Remove DTO dependency từ Collection model | `Models/Collection.cs`, `Services/CollectionService.cs` | ⬜ |
| 7 | Remove DTO dependency từ Tag model | `Models/Tag.cs`, `Services/TagService.cs` | ⬜ |

#### Phase B2: Service Layer OOP (HIGH/MEDIUM priority)
| # | Task | Files | Status |
|---|------|-------|--------|
| 8 | Extract `IAccessControlService` — centralize permission checks | New service, `AssetService`, `BulkAssetService` | ⬜ |
| 9 | Extract `IFileValidator` — tách validation logic từ AssetService | New service, `AssetService` | ⬜ |
| 10 | Fix N+1 query trong `PermissionService.ListAsync` | `Services/PermissionService.cs` | ⬜ |
| 11 | Extract sorting extension — `IQueryable<Asset>.ApplySort(pagination)` | New extension, 3 services | ⬜ |
| 12 | Extract `CacheKeys` — shared constants thay magic strings | New class, `CollectionService`, `PermissionService` | ⬜ |
| 13 | `IAssetCleanupHelper` interface (hiện là concrete class) | `Services/AssetCleanupHelper.cs` | ⬜ |
| 14 | `FileUploadConfig` → `IOptions<FileUploadConfig>` pattern | `Common.cs`, `ServiceCollectionExtensions.cs` | ⬜ |

#### Phase B3: Infrastructure OOP (MEDIUM/LOW priority)
| # | Task | Files | Status |
|---|------|-------|--------|
| 15 | `IEntityTypeConfiguration<T>` — tách AppDbContext.OnModelCreating 170 dòng | `Data/AppDbContext.cs`, new config files | ⬜ |
| 16 | `HubEvents` constants — sử dụng thay string literals | `Hubs/AssetHub.cs`, tất cả services gọi NotifyAsync | ⬜ |
| 17 | `CollectionRoles` → enum hoặc smart enum thay string constants | `Models/CollectionPermission.cs`, services | ⬜ |
| 18 | Raw SQL migration logic → tách service hoặc EF migration | `Program.cs` | ⬜ |
| 19 | HealthController → inject IHealthCheckService | `Controllers/HealthController.cs` | ⬜ |
| 20 | `PostCollection(Collection)` → `PostCollection(CreateCollectionDto)` | `Controllers/CollectionsController.cs` | ✅ Session #6.4 |
| 21 | Enriched TPH subtypes — `LinkAsset.ValidateUrl()`, `ColorAsset.ValidateCode()` | `Models/AssetTypes.cs` | ⬜ |
| 22 | Mega DTOs.cs → tách per-domain (Asset DTOs, Tag DTOs, Bulk DTOs, Collection DTOs) | `Models/DTOs.cs` | ⬜ |

---

### Session #6.5 — C# 12 Primary Constructors & Expression-bodied Members (01/03/2026)

**Mục tiêu:** Modernize tất cả 9 controllers bằng C# 12 Primary Constructor syntax + expression-bodied members (`=>`).

#### Thay đổi áp dụng cho MỖI controller:

1. **Primary Constructor** — loại bỏ `private readonly` field + constructor body → inject trực tiếp vào class declaration
   - Trước: `public class XController : BaseApiController { private readonly IService _svc; public XController(IService svc) { _svc = svc; } }`
   - Sau: `public class XController(IService svc) : BaseApiController { ... }`
   - Giảm ~5 dòng boilerplate / controller

2. **Expression-bodied members (`=>`)** — single-statement methods chuyển sang expression form
   - Trước: `{ var x = await svc.Get(); return Ok(x); }`
   - Sau: `=> Ok(await svc.Get());`
   - Chỉ áp dụng khi method body là 1 statement duy nhất

3. **Block body giữ nguyên** cho:
   - `CreatedAtAction()` (cần biến trung gian)
   - `NoContent()` sau `await` (2 statements)
   - Complex logic (try/catch, anonymous objects)

#### 9 Controllers đã refactor:

| Controller | Services | Primary Ctor | Expression-bodied methods |
|-----------|----------|-------------|--------------------------|
| `AssetsController` | `IAssetService` | ✅ | 6/15 (GetAssets, GetById, UpdateAsset, UpdatePosition, GetByGroup, UploadFiles) |
| `BulkAssetsController` | `IBulkAssetService` | ✅ | 0/4 (all have 2-stmt body) |
| `CollectionsController` | `ICollectionService` | ✅ | 2/6 (GetCollections, GetWithItems) |
| `AuthController` | `IAuthService` | ✅ | 2/2 (Register, Login) |
| `TagsController` | `ITagService` | ✅ | 4/11 (GetTags, GetTag, UpdateTag, GetAssetTags) |
| `PermissionsController` | `IPermissionService` | ✅ | 4/7 (List, Grant, Update, GetSharedCollections) |
| `SearchController` | `ISearchService` | ✅ | 1/1 (Search) |
| `SmartCollectionsController` | `ISmartCollectionService` | ✅ | 2/2 (GetSmartCollections, GetSmartCollectionItems) |
| `HealthController` | `AppDbContext`, `IWebHostEnvironment` | ✅ | 0/1 (complex try/catch) |

**Tổng:** 9/9 controllers → primary constructor, 21 methods → expression-bodied

#### Kết quả build:
```
dotnet build --no-restore → 0 errors, 0 warnings ✅
```

---

### Session #6.6 — Domain Controller Separation & Policy-Based Authorization (01/03/2026)

**Mục tiêu:** Phân rã AssetsController theo Domain-Driven Design (SRP) + nâng cấp từ `[Authorize]` → Policy-based authorization.

#### 1. Modular Domain Separation (SRP)

Tách 4 specialized asset creation endpoints ra khỏi AssetsController thành các domain-specific controllers:

| Controller mới | Route | Domain | Endpoints |
|---------------|-------|--------|-----------|
| `FoldersController` | `api/assets/folders` | Folder (organizational container) | `POST` — create folder |
| `ColorsController` | `api/assets/colors` | Color (single swatch) | `POST` — create color |
| `ColorGroupsController` | `api/assets/color-groups` | ColorGroup (palette container) | `POST` — create color group |
| `LinksController` | `api/assets/links` | Link (external URL bookmark) | `POST` — create link |

**AssetsController** giữ lại 10 endpoints core lifecycle:
- **Reads (3):** `GET /`, `GET /{id}`, `GET /group/{groupId}`
- **Writes (7):** `POST /`, `POST /upload`, `PATCH /{id}`, `PUT /{id}`, `PUT /{id}/position`, `DELETE /{id}`, `POST /{id}/duplicate`, `POST /reorder`

**Frontend không cần thay đổi** — route URLs giữ nguyên (controller routes match existing paths).

#### 2. Policy-Based Authorization

Thay thế `[Authorize]` chung bằng granular policies:

| Policy | Áp dụng cho | Controllers |
|--------|-----------|------------|
| `RequireAssetRead` | `GET` endpoints | `AssetsController` (3 endpoints) |
| `RequireAssetWrite` | `POST/PUT/PATCH/DELETE` | `AssetsController` (7), `BulkAssetsController` (4), `FoldersController`, `ColorsController`, `ColorGroupsController`, `LinksController` |

Policies registered trong `ServiceCollectionExtensions.AddIdentityAndAuth()` via `AddAuthorizationBuilder()`.

#### 3. HTTP Semantics (đã có từ trước, xác nhận consistency)

| Action | Status Code | Pattern |
|--------|------------|---------|
| Create (POST) | `201 Created` | `CreatedAtAction()` + Location header |
| Update (PATCH/PUT) | `200 OK` | Return updated resource |
| Delete | `204 No Content` | No body |
| Reorder | `204 No Content` | No body |

#### Files đã thay đổi (Session #6.6)

| File | Action |
|------|--------|
| `Controllers/AssetsController.cs` | Removed 4 specialized creation endpoints, added per-action policy auth |
| `Controllers/FoldersController.cs` | **NEW** — folder creation domain controller |
| `Controllers/ColorsController.cs` | **NEW** — color creation domain controller |
| `Controllers/ColorGroupsController.cs` | **NEW** — color group creation domain controller |
| `Controllers/LinksController.cs` | **NEW** — link creation domain controller |
| `Controllers/BulkAssetsController.cs` | `[Authorize]` → `[Authorize(Policy = "RequireAssetWrite")]` |
| `Extensions/ServiceCollectionExtensions.cs` | Added `RequireAssetRead` + `RequireAssetWrite` policies |

#### Kết quả build:
```
dotnet build --no-restore → 0 errors, 0 warnings ✅
```

#### Phase F1: Domain Model Activation
| # | Task | Files | Status |
|---|------|-------|--------|
| 1 | API services return model instances — `toAsset()`, `toCollection()`, `toTag()` | `api/*.js`, `models/index.js` | ⬜ |
| 2 | Remove backward-compat free function exports | `api/*.js` | ⬜ |
| 3 | Use enum constants (`ContentType.Image`) thay raw strings | `models/index.js`, 15+ components | ⬜ |

#### Phase F2: God Component/Context Split
| # | Task | Files | Status |
|---|------|-------|--------|
| 4 | Split AppContext (471 dòng) → domain contexts | `context/AppContext.js` | ⬜ |
| 5 | Split App.jsx (477 dòng) → composition | `App.jsx` | ⬜ |
| 6 | Split ColorBoard.jsx (556 dòng) → subcomponents | `components/ColorBoard.jsx` | ⬜ |

#### Phase F3: DRY + Encapsulation
| # | Task | Files | Status |
|---|------|-------|--------|
| 7 | Extract shared ContextMenuBuilder | 3 components | ⬜ |
| 8 | Extract shared IconMapper | 5 components | ⬜ |
| 9 | Remove direct API calls from components | DraggableAssetCanvas, TreeViewPanel, App.jsx | ⬜ |
| 10 | Encapsulate hook state — hide raw setters | useAssetSelection, useCollections, useAuth | ⬜ |

---

### Session #6.7 — Production Architecture: DTO Boundary, API Versioning & Layout Separation (01/03/2026)

**Mục tiêu:** 6 nâng cấp kiến trúc production-grade: DTO boundary, API versioning, layout SRP, explicit binding, validation, required contracts.

#### 1. Clean Architecture — DTO Boundary

Thay thế `Asset` entity bằng `AssetResponseDto` trong tất cả API response types:

| Component | Thay đổi |
|-----------|----------|
| `AssetResponseDto` | **NEW** — 19 properties (Id, FileName, FilePath, Tags, CreatedAt, PositionX/Y, CollectionId, ContentType, GroupId, ParentFolderId, SortOrder, IsFolder, Thumbnail Sm/Md/Lg) |
| `Asset.ToDto()` | **NEW** — instance mapping method trên entity |
| `IAssetService` | All 14 return types: `Asset` → `AssetResponseDto`, `List<Asset>` → `List<AssetResponseDto>`, `PagedResult<Asset>` → `PagedResult<AssetResponseDto>` |
| `AssetService` | Added `.ToDto()` / `.Select(a => a.ToDto()).ToList()` trên mọi return statement |
| `ISmartCollectionService` | `PagedResult<Asset>` → `PagedResult<AssetResponseDto>` |
| `SmartCollectionService` | Items mapped via `.ToDto()` |
| `SearchService` | `SearchResult.Assets` mapped via `.Select(a => a.ToDto())` |
| `CollectionService` | `CollectionWithItemsResult.Items` mapped via `.Select(a => a.ToDto())` |
| `SearchResult` DTO | `List<Asset>` → `List<AssetResponseDto>` |
| `CollectionWithItemsResult` DTO | `List<Asset>` → `List<AssetResponseDto>` |

**Lợi ích:** Entity internals (UserId, navigation properties) không bao giờ lộ ra API response.

#### 2. Layout vs Lifecycle Separation (SRP)

| Từ | Đến |
|----|-----|
| `AssetsController.UpdatePosition()` | **→ `AssetLayoutController`** |
| `AssetsController.ReorderAssets()` | **→ `AssetLayoutController`** |

`AssetLayoutController` — route `api/v1/assets`, chứa 2 endpoints:
- `PUT {id}/position` — update canvas position
- `POST reorder` — reorder assets

`AssetsController` giảm từ 10 → 8 endpoints (chỉ lifecycle).

#### 3. API Versioning — `api/v1/[controller]`

Tất cả 13 controllers đã chuyển sang versioned routes:

| Controller | Route cũ | Route mới |
|-----------|---------|-----------|
| `AssetsController` | `api/[controller]` | `api/v1/[controller]` |
| `AssetLayoutController` | — | `api/v1/assets` (NEW) |
| `BulkAssetsController` | `api/assets` | `api/v1/assets` |
| `FoldersController` | `api/assets/folders` | `api/v1/assets/folders` |
| `ColorsController` | `api/assets/colors` | `api/v1/assets/colors` |
| `ColorGroupsController` | `api/assets/color-groups` | `api/v1/assets/color-groups` |
| `LinksController` | `api/assets/links` | `api/v1/assets/links` |
| `CollectionsController` | `api/[controller]` | `api/v1/[controller]` |
| `AuthController` | `api/[controller]` | `api/v1/[controller]` |
| `TagsController` | `api/[controller]` | `api/v1/[controller]` |
| `PermissionsController` | `api/collections/{id}/permissions` | `api/v1/collections/{id}/permissions` |
| `SearchController` | `api/[controller]` | `api/v1/[controller]` |
| `SmartCollectionsController` | `api/[controller]` | `api/v1/[controller]` |
| `HealthController` | `api/[controller]` | `api/v1/[controller]` |

**Frontend:** `client.js` baseURL updated `'/api'` → `'/api/v1'`.
**PermissionsController:** absolute route `/api/shared-collections` → `/api/v1/shared-collections`.

#### 4. Explicit Binding — `[FromForm]`

`UploadFiles` endpoint: `List<IFormFile> files` → `[FromForm] List<IFormFile> files`.

#### 5. Required Contract — `DuplicateAssetDto`

`DuplicateAssetDto? dto = null` → `DuplicateAssetDto dto` (non-nullable, required body).
Access changed: `dto?.TargetFolderId` → `dto.TargetFolderId`.

#### 6. Parameter Validation — `[Range(1, int.MaxValue)]`

Applied `[Range(1, int.MaxValue)]` trên tất cả `int id` parameters trong `AssetsController` và `AssetLayoutController`:
- `GetAssetById(int id)`, `GetAssetsByGroup(int groupId)`
- `UpdateAsset(int id)`, `UpdateAssetPut(int id)`, `DeleteAsset(int id)`
- `DuplicateAsset(int id)`, `UpdatePosition(int id)`

#### Files đã thay đổi (Session #6.7)

| File | Action |
|------|--------|
| `Models/DTOs.cs` | Added `AssetResponseDto`; updated `SearchResult.Assets` + `CollectionWithItemsResult.Items` to use DTO |
| `Models/Asset.cs` | Added `ToDto()` mapping method |
| `Services/IAssetService.cs` | All return types → `AssetResponseDto` variants |
| `Services/AssetService.cs` | All returns mapped via `.ToDto()` |
| `Services/ISmartCollectionService.cs` | Return type → `PagedResult<AssetResponseDto>` |
| `Services/SmartCollectionService.cs` | Items mapped via `.ToDto()` |
| `Services/SearchService.cs` | Assets mapped via `.Select(a => a.ToDto())` |
| `Services/CollectionService.cs` | Items mapped via `.Select(a => a.ToDto())` |
| `Controllers/AssetsController.cs` | Rewritten: v1 route, DTO types, [Range], [FromForm], required DuplicateAssetDto, removed layout endpoints |
| `Controllers/AssetLayoutController.cs` | **NEW** — UpdatePosition + ReorderAssets with v1 route |
| `Controllers/BulkAssetsController.cs` | Route → `api/v1/assets` |
| `Controllers/FoldersController.cs` | Route → `api/v1/assets/folders`, `AssetResponseDto` types |
| `Controllers/ColorsController.cs` | Route → `api/v1/assets/colors`, `AssetResponseDto` types |
| `Controllers/ColorGroupsController.cs` | Route → `api/v1/assets/color-groups`, `AssetResponseDto` types |
| `Controllers/LinksController.cs` | Route → `api/v1/assets/links`, `AssetResponseDto` types |
| `Controllers/CollectionsController.cs` | Route → `api/v1/[controller]` |
| `Controllers/AuthController.cs` | Route → `api/v1/[controller]` |
| `Controllers/TagsController.cs` | Route → `api/v1/[controller]` |
| `Controllers/PermissionsController.cs` | Route → `api/v1/...`, absolute → `/api/v1/shared-collections` |
| `Controllers/SearchController.cs` | Route → `api/v1/[controller]` |
| `Controllers/SmartCollectionsController.cs` | Route → `api/v1/[controller]`, `AssetResponseDto` types |
| `Controllers/HealthController.cs` | Route → `api/v1/[controller]` |
| `Frontend/src/api/client.js` | baseURL → `'/api/v1'` |

#### Kết quả build:
```
dotnet build --no-restore → 0 errors, 0 warnings ✅
```

---

### Session #6.8 — CQRS (MediatR), GlobalExceptionHandler (RFC 7807) & API Contract (01/03/2026)

**Mục tiêu:** Tách Fat Service khỏi Controller bằng CQRS pattern (MediatR), chuẩn hóa Global Exception Handling với `IExceptionHandler`, và làm sắc nét API Contract.

#### 1. CQRS Pattern (MediatR 12.5)

**Trước:** `AssetsController(IAssetService assetService)` — controller phụ thuộc trực tiếp vào Fat Service.
**Sau:** `AssetsController(ISender sender)` — controller chỉ là Dispatcher, gửi Command/Query qua MediatR pipeline.

| Layer | Files | Vai trò |
|-------|-------|---------|
| **Queries** | `CQRS/Assets/Queries/AssetQueries.cs` | `GetAssetsQuery`, `GetAssetByIdQuery`, `GetAssetsByGroupQuery` — sealed records implement `IRequest<T>` |
| **Commands** | `CQRS/Assets/Commands/AssetCommands.cs` | `CreateAssetCommand`, `UploadFilesCommand`, `UpdateAssetCommand`, `DeleteAssetCommand`, `DuplicateAssetCommand` — sealed records implement `IRequest<T>` |
| **Query Handlers** | `CQRS/Assets/Handlers/AssetQueryHandlers.cs` | 3 handlers, delegate to `IAssetService` (incremental refactoring) |
| **Command Handlers** | `CQRS/Assets/Handlers/AssetCommandHandlers.cs` | 5 handlers, delegate to `IAssetService` |

**DI Registration:** `services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<AssetService>())` trong `AddApplicationServices()`.

**Lợi ích:** Controller zero logic (pure dispatcher), pipeline behaviors (logging, validation, caching) có thể inject later, rõ ràng Read vs Write intent.

#### 2. Explicit Duplicate Routing (API Contract)

**Trước:** `POST /{id}/duplicate` + `[FromBody] DuplicateAssetDto dto` (lỏng lẻo, body optional).
**Sau:** 2 endpoint rõ ràng, không cần request body:

| Endpoint | Mục đích |
|----------|----------|
| `POST /{id}/duplicate` | Nhân bản tại chỗ (same folder) |
| `POST /{id}/duplicate-to-folder/{folderId}` | Nhân bản sang thư mục khác |

**Class `DuplicateAssetDto` đã bị xoá** khỏi DTOs.cs.
**Frontend** `assetsApi.js`: `duplicateAsset()` updated — sử dụng URL routing thay vì request body.

#### 3. Global Exception Handler — RFC 7807 ProblemDetails

**Trước:** `ExceptionHandlingMiddleware` (legacy middleware pattern, manual JSON serialization).
**Sau:** `GlobalExceptionHandler` implements `IExceptionHandler` (ASP.NET Core 8+ built-in interface).

| Exception | HTTP Status | ProblemDetails Title |
|-----------|------------|---------------------|
| `NotFoundException` | 404 | "Not Found" |
| `ValidationException` | 400 | "Validation Failed" (+ per-field `errors` extension) |
| `ArgumentException` | 400 | "Bad Request" |
| `KeyNotFoundException` | 404 | "Not Found" |
| `UnauthorizedAccessException` | 401 | "Unauthorized" |
| `InvalidOperationException` | 409 | "Conflict" |
| Fallback | 500 | "Internal Server Error" (detail hidden in Production) |

Mọi response đều có `traceId` extension. Registered via `services.AddExceptionHandler<GlobalExceptionHandler>()` + `services.AddProblemDetails()`.
Pipeline: `app.UseExceptionHandler()` thay cho `app.UseGlobalExceptionHandler()`.

**Custom Exceptions** (mới):
- `Exceptions/NotFoundException.cs` — semantic 404, constructor overloads: `(string message)` + `(string entityName, object key)`
- `Exceptions/ValidationException.cs` — semantic 400, carries `IDictionary<string, string[]> Errors` for per-field validation

#### 4. PaginationParams Validation (Data Annotations)

**Trước:** Runtime clamping via `Math.Min(value, MaxPageSize)` — silent mutation.
**Sau:** Explicit Data Annotations — framework auto-validates before handler runs:

| Property | Annotation | Description |
|----------|-----------|-------------|
| `Page` | `[Range(1, int.MaxValue)]` | Must be ≥ 1 |
| `PageSize` | `[Range(1, 100)]` | Must be 1–100 |

Invalid values → automatic 400 ValidationProblemDetails (via `[ApiController]`).

#### Files đã thay đổi (Session #6.8)

| File | Action |
|------|--------|
| `VAH.Backend.csproj` | Added `MediatR 12.5.0` NuGet package |
| `Exceptions/NotFoundException.cs` | **NEW** — custom 404 exception |
| `Exceptions/ValidationException.cs` | **NEW** — custom 400 exception with per-field errors |
| `CQRS/Assets/Queries/AssetQueries.cs` | **NEW** — 3 query records |
| `CQRS/Assets/Commands/AssetCommands.cs` | **NEW** — 5 command records |
| `CQRS/Assets/Handlers/AssetQueryHandlers.cs` | **NEW** — 3 query handlers |
| `CQRS/Assets/Handlers/AssetCommandHandlers.cs` | **NEW** — 5 command handlers |
| `Controllers/AssetsController.cs` | Rewritten: `IAssetService` → `ISender`, CQRS dispatch, 2 explicit duplicate routes |
| `Middleware/GlobalExceptionHandler.cs` | **NEW** — `IExceptionHandler` implementation (RFC 7807) |
| `Models/Common.cs` | `PaginationParams` rewritten with `[Range]` annotations |
| `Models/DTOs.cs` | Removed `DuplicateAssetDto` class |
| `Extensions/ServiceCollectionExtensions.cs` | Added MediatR registration + `AddExceptionHandler` + `AddProblemDetails` |
| `Program.cs` | `app.UseGlobalExceptionHandler()` → `app.UseExceptionHandler()` |
| `Frontend/src/api/assetsApi.js` | `duplicateAsset()` uses URL routing instead of request body |

#### Kết quả build:
```
dotnet build --no-incremental → 0 errors, 0 warnings ✅
```