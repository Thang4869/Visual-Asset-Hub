# Tiêu Chuẩn Code Frontend (Coding Standards — React 19 Frontend)

> **Mục đích**: Định nghĩa quy tắc và chuẩn mực code cho frontend React 19  
> **Last Updated**: 2026-04-06  
> **Áp dụng cho**: `src/VAH.Frontend/src/`

---

## §1 — Cấu Trúc Dự Án

```
src/
├── api/                        # Class-based OOP API layer
│   ├── BaseApiService.js       #   Abstract base (shared CRUD helpers)
│   ├── TokenManager.js         #   Singleton (JWT lifecycle)
│   ├── client.js               #   Axios instance + interceptors
│   ├── index.js                #   Barrel exports
│   ├── assetsApi.js            #   extends BaseApiService
│   ├── authApi.js              #   extends BaseApiService
│   ├── collectionsApi.js       #   extends BaseApiService
│   ├── permissionsApi.js       #   extends BaseApiService
│   ├── searchApi.js            #   extends BaseApiService
│   ├── smartCollectionsApi.js  #   extends BaseApiService
│   └── tagsApi.js              #   extends BaseApiService
├── hooks/                      # Custom hooks (1 concern per hook)
│   ├── useAuth.js
│   ├── useAssets.js
│   ├── useAssetSelection.js
│   ├── useBulkOperations.js
│   ├── useCollections.js
│   ├── useCollectionNavigation.js
│   ├── useSharePermissions.js
│   ├── useSignalR.js
│   ├── useSmartCollections.js
│   ├── useTags.js
│   └── useUndoRedo.js
├── context/                    # React Context (global state)
│   ├── AppContext.js
│   └── ConfirmContext.js
├── models/                     # Domain model classes
│   └── index.js
├── components/                 # UI components (JSX + CSS pairs)
│   ├── AppHeader.jsx
│   ├── AppSidebar.jsx
│   ├── AssetGrid.jsx / .css
│   └── ... (17 total)
├── App.jsx / App.css
├── main.jsx / index.css
└── assets/
```

---

## §2 — Tầng API (OOP)

### Cấu Trúc Kế Thừa Class
```
BaseApiService          ← Abstract base: _get(), _post(), _put(), _patch(), _delete()
├── AssetsApi           ← Asset CRUD + upload + duplicate
├── AuthApi             ← Login + register
├── CollectionsApi      ← Collection CRUD + tree
├── TagsApi             ← Tag CRUD + asset-tag management
├── SearchApi           ← Full-text search
├── SmartCollectionsApi ← Virtual collections
└── PermissionsApi      ← RBAC sharing
```

### Quy Tắc

| Quy tắc | Mức độ |
|---------|--------|
| Mọi API service phải extends `BaseApiService` | `[MUST]` |
| Sử dụng `_get()`, `_post()` helpers — không dùng trực tiếp `axios.get()` | `[MUST]` |
| Export singleton instance (không export class) | `[MUST]` |
| Quản lý token chỉ qua `TokenManager` singleton | `[MUST]` |
| Không import `axios` trực tiếp trong components/hooks | `[MUST]` |

### JSDoc Template cho API Service
```javascript
/**
 * @class AssetsApi
 * @extends BaseApiService
 * @description Manages asset CRUD, upload, and layout operations.
 */
export class AssetsApi extends BaseApiService {
  /**
   * @param {number} id - Asset ID
   * @returns {Promise<AssetResponseDto>} The asset data
   * @throws {AxiosError} 404 if not found
   */
  async getById(id) { return this._get(`/${id}`); }
}
```

---

## §3 — Custom Hooks

### Nguyên Tắc SRP cho Hooks
Mỗi hook quản lý đúng MỘT concern:

| Hook | Concern | API Service Sử Dụng |
|------|---------|---------------------|
| `useAuth` | Auth state, login/logout | `authApi` |
| `useAssets` | Asset CRUD, loading state | `assetApi` |
| `useAssetSelection` | Multi-select state | None (local state) |
| `useBulkOperations` | Batch delete/move/tag | `assetApi` |
| `useCollections` | Collection CRUD | `collectionApi` |
| `useCollectionNavigation` | Active collection routing | None |
| `useSharePermissions` | RBAC grant/revoke | `permissionApi` |
| `useSignalR` | WebSocket connection | None (SignalR client) |
| `useSmartCollections` | Virtual collections | `smartCollectionApi` |
| `useTags` | Tag CRUD, asset-tag ops | `tagApi` |
| `useUndoRedo` | Command history | None (local state) |

### Quy Tắc

| Quy tắc | Mức độ |
|---------|--------|
| 1 hook = 1 concern (SRP) | `[MUST]` |
| Hook gọi API service, không dùng raw `axios` | `[MUST]` |
| Hook trả về `{ data, isLoading, error, actions }` | `[SHOULD]` |
| Không thao tác DOM trực tiếp | `[MUST]` |

---

## §4 — Components

### Quy Tắc

| Quy tắc | Mức độ |
|---------|--------|
| Component = chỉ render UI. Logic nằm trong hooks | `[MUST]` |
| CSS file đặt cùng thư mục với component | `[SHOULD]` |
| Props destructured trong function signature | `[SHOULD]` |
| `ErrorBoundary` bao component tree | `[MUST]` |
| Không inline styles > 2 properties | `[SHOULD]` |

### JSDoc Template
```javascript
/**
 * @component AssetGrid
 * @description Renders assets in a grid/canvas layout.
 * @param {Object} props
 * @param {Asset[]} props.assets - Array of assets to display
 * @param {Function} props.onSelect - Selection callback
 * @param {string} props.layout - 'grid' | 'list' | 'canvas'
 */
export default function AssetGrid({ assets, onSelect, layout }) { ... }
```

---

## §5 — Quản Lý State

```
AppContext (global)
├── user, isAuthenticated
├── collections, activeCollectionId
├── assets, selectedAssetIds
├── tags
└── UI state (sidebarOpen, theme)

ConfirmContext (dialog)
├── isOpen, message, onConfirm, onCancel
```

**Quy tắc**: Không prop-drilling quá 2 cấp. Dùng Context cho global state. Dùng hook cho domain logic. Component dispatch actions qua hooks, không sửa context trực tiếp.

---

## §6 — Quy Ước Đặt Tên

| Thành phần | Quy ước | Ví dụ |
|------------|---------|-------|
| Component | PascalCase | `AssetGrid.jsx` |
| Hook | `use{Domain}` camelCase | `useAssets.js` |
| API service | `{domain}Api` camelCase | `assetsApi.js` |
| CSS file | Cùng tên với component | `AssetGrid.css` |
| Context | `{Name}Context` | `AppContext.js` |
| Constants | UPPER_SNAKE | `STATIC_URL` |
| Event handler | `handle{Event}` | `handleClick`, `handleDragEnd` |

---

> **Document End**
