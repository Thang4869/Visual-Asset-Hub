# API LAYER — Frontend HTTP Client Architecture

> **Last Updated**: 2026-03-02

---

## §1 — Architecture Overview

```
Components → Hooks → API Services → Axios Client → Backend REST API
                          │
                    BaseApiService (abstract base)
                    ├── AssetsApi        /api/v1/Assets
                    ├── AuthApi          /api/v1/auth
                    ├── CollectionsApi   /api/v1/collections
                    ├── TagsApi          /api/v1/tags
                    ├── SearchApi        /api/v1/search
                    ├── SmartCollApi     /api/v1/smart-collections
                    └── PermissionsApi   /api/v1/permissions
```

## §2 — Core Files

### client.js — Axios Instance

```javascript
const apiClient = axios.create({
    baseURL: `${STATIC_URL}/api/v1`,
    headers: { 'Content-Type': 'application/json' }
});
```

**Interceptors:**
- **Request**: Attaches `Authorization: Bearer <token>` from `TokenManager`
- **Response (401)**: Auto-clears token and redirects to login

**Exports:**
- `apiClient` — configured Axios instance
- `staticUrl` / `STATIC_URL` — base URL for static file references

### TokenManager.js — JWT Token Singleton

```javascript
class TokenManager {
    #storageKey = 'vah_token';   // Private field (encapsulation)

    getToken()    → string | null
    setToken(t)   → void
    clearToken()  → void
    isLoggedIn()  → boolean
}
export default new TokenManager();   // Singleton instance
```

**OOP Patterns:**
- **Singleton** — single export instance
- **Encapsulation** — private `#storageKey` field
- Uses `localStorage` for persistence

### BaseApiService.js — Abstract Base Class

```javascript
export default class BaseApiService {
    endpoint;    // Set by subclass constructor
    client;      // Shared axios instance

    constructor(endpoint) {
        this.endpoint = endpoint;
        this.client = apiClient;
    }

    async _get(path, params)     → response.data
    async _post(path, data, cfg) → response.data
    async _put(path, data)       → response.data
    async _patch(path, data)     → response.data
    async _delete(path)          → response.data
}
```

All helpers auto-unwrap `response.data` for cleaner consumption.

## §3 — API Service Classes

### AssetsApi

```javascript
class AssetsApi extends BaseApiService {
    constructor() { super('/Assets'); }

    getAll(collectionId, params)
    getById(id)
    getByFolder(collectionId, folderId, params)
    upload(collectionId, formData, onProgress)
    update(id, data)
    delete(id)
    duplicate(id, targetFolderId?)
    updatePosition(id, x, y)
    reorderAssets(collectionId, orderedIds)
    // Bulk operations
    bulkDelete(assetIds)
    bulkMove(assetIds, targetCollectionId)
    bulkMoveToGroup(assetIds, groupId)
    bulkTag(assetIds, tagIds)
    // Specialized creators
    createFolder(data)
    createColor(data)
    createColorGroup(data)
    createLink(data)
}
```

### CollectionsApi

```javascript
class CollectionsApi extends BaseApiService {
    constructor() { super('/collections'); }

    getAll()
    getById(id)
    fetchItems(id, folderId?)
    create(data)
    update(id, data)
    delete(id)
}
```

### TagsApi

```javascript
class TagsApi extends BaseApiService {
    constructor() { super('/tags'); }

    getAll()
    create(data)
    update(id, data)
    delete(id)
    getOrCreate(tagNames)
    setAssetTags(assetId, tagIds)
    addAssetTags(assetId, tagIds)
    removeAssetTags(assetId, tagIds)
    getAssetTags(assetId)
}
```

### Other Services

| Service | Endpoint | Key Methods |
|---------|----------|-------------|
| `AuthApi` | `/auth` | `register(dto)`, `login(dto)` |
| `SearchApi` | `/search` | `search(query, type?, collectionId?, page?, pageSize?)` |
| `SmartCollectionsApi` | `/smart-collections` | `getDefinitions()`, `getItems(id, params)` |
| `PermissionsApi` | `/permissions` | `list(colId)`, `grant(colId, dto)`, `update(id, dto)`, `revoke(id)`, `getMyRole(colId)`, `getShared()` |

## §4 — Barrel Export

```javascript
// src/api/index.js
export { default as assetApi } from './assetsApi';
export { default as authApi } from './authApi';
export { default as collectionApi } from './collectionsApi';
export { default as tagApi } from './tagsApi';
export { default as searchApi } from './searchApi';
export { default as smartCollectionApi } from './smartCollectionsApi';
export { default as permissionApi } from './permissionsApi';
export { default as tokenManager } from './TokenManager';
export { staticUrl, STATIC_URL } from './client';
```

All exports are **singleton instances** — one API service per domain.

## §5 — OOP Patterns Summary

| Pattern | Implementation |
|---------|---------------|
| **Inheritance** | All API services extend `BaseApiService` |
| **Encapsulation** | `TokenManager.#storageKey` (private field) |
| **Singleton** | Each API service exported as `new XxxApi()` |
| **Template Method** | `_get`, `_post` etc. are template methods that subclasses compose |
| **Open/Closed** | Add new API service by extending `BaseApiService` — no base modification |

---

> **Document End**
