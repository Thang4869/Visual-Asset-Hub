# CODING STANDARDS — React 19 Frontend

> **Last Updated**: 2026-03-02  
> **Applies to**: `VAH.Frontend/src/`

---

## §1 — Project Structure

```
src/
├── api/                    # Class-based OOP API layer
│   ├── BaseApiService.js   #   Abstract base (shared CRUD helpers)
│   ├── TokenManager.js     #   Singleton (JWT lifecycle)
│   ├── client.js           #   Axios instance + interceptors
│   ├── index.js            #   Barrel exports
│   ├── assetsApi.js        #   extends BaseApiService
│   ├── authApi.js          #   extends BaseApiService
│   ├── collectionsApi.js   #   extends BaseApiService
│   ├── permissionsApi.js   #   extends BaseApiService
│   ├── searchApi.js        #   extends BaseApiService
│   ├── smartCollectionsApi.js  #   extends BaseApiService
│   └── tagsApi.js          #   extends BaseApiService
├── hooks/                  # Custom hooks (1 concern per hook)
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
├── context/                # React Context (global state)
│   ├── AppContext.js
│   └── ConfirmContext.js
├── models/                 # Domain model classes
│   └── index.js
├── components/             # UI components (JSX + CSS pairs)
│   ├── AppHeader.jsx
│   ├── AppSidebar.jsx
│   ├── AssetGrid.jsx / .css
│   └── ... (17 total)
├── App.jsx / App.css
├── main.jsx / index.css
└── assets/
```

## §2 — API Layer (OOP)

### Class Hierarchy
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

### Rules

| Rule | Severity |
|------|----------|
| Every API service extends `BaseApiService` | `[MUST]` |
| Use `_get()`, `_post()` helpers — never raw `axios.get()` | `[MUST]` |
| Export singleton instance (not class) | `[MUST]` |
| Token management via `TokenManager` singleton only | `[MUST]` |
| Never import `axios` directly in components/hooks | `[MUST]` |

### JSDoc Template for API Service
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

## §3 — Custom Hooks

### SRP for Hooks
Each hook manages exactly ONE concern:

| Hook | Concern | API Service Used |
|------|---------|-----------------|
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

### Rules

| Rule | Severity |
|------|----------|
| 1 hook = 1 concern (SRP) | `[MUST]` |
| Hook calls API service, never raw `axios` | `[MUST]` |
| Hook returns `{ data, isLoading, error, actions }` | `[SHOULD]` |
| No direct DOM manipulation | `[MUST]` |

## §4 — Components

### Rules

| Rule | Severity |
|------|----------|
| Component = UI rendering only. Logic lives in hooks | `[MUST]` |
| Co-located CSS file per component | `[SHOULD]` |
| Props destructured in function signature | `[SHOULD]` |
| `ErrorBoundary` wraps component tree | `[MUST]` |
| No inline styles > 2 properties | `[SHOULD]` |

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

## §5 — State Management

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

**Rules**: No prop-drilling beyond 2 levels. Use Context for global state. Use hook for domain logic. Component dispatches actions via hooks, never modifies context directly.

## §6 — Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Component | PascalCase | `AssetGrid.jsx` |
| Hook | `use{Domain}` camelCase | `useAssets.js` |
| API service | `{domain}Api` camelCase | `assetsApi.js` |
| CSS file | Same name as component | `AssetGrid.css` |
| Context | `{Name}Context` | `AppContext.js` |
| Constants | UPPER_SNAKE | `STATIC_URL` |
| Event handler | `handle{Event}` | `handleClick`, `handleDragEnd` |

---

> **Document End**
