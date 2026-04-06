# QUẢN LÝ TRẠNG THÁI — Kiến trúc Context & Hooks

> **Last Updated**: 2026-04-06
> **Ghi chú**: Khớp với implementation hiện tại trong `VAH.Frontend/src/` (AppContext được compose từ domain hooks). Giữ AppContext làm orchestrator ở quy mô hiện tại.
--- 
## §1 — Tổng quan kiến trúc

VAH Frontend uses **React Context + Custom Hooks** for state management (no Redux/Zustand):

```
                    ConfirmContext
                         │
                    AppContext (composes all hooks)
                    ┌────┼─────┬──────┬──────┬──────┬───────┐
                    │    │     │      │      │      │       │
               useAuth  useCol useAss useTags useSR useUndo useSmart
                    │    │     │      │      │      │       │
                    └────┴─────┴──────┴──────┴──────┴───────┘
                                   │
                          useAppContext() ← consumed by components
```

## §2 — Context Providers

### AppContext (`AppContext.js`)

Context trung tâm gom toàn bộ domain hooks thành một provider duy nhất. Loại bỏ prop-drilling trên toàn bộ component tree.

**Composed State:**
- `auth` — `useAuth()` → login state, token, user info
- `collectionState` — `useCollections()` → CRUD, navigation, current folder
- `assetState` — `useAssets()` → selection, operations, current items
- `tagState` — `useTags()` → tag CRUD
- `smartState` — `useSmartCollections()` → smart collection definitions
- `undoRedo` — `useUndoRedo()` → undo/redo stack
- View state: `viewMode`, `layoutMode`, `searchTerm`, `debouncedSearch`
- UI state: `clipboard`, `pinnedItems`, `selectedFolderIds`, `showShareDialog`

**Key Pattern:** Each hook manages its own async loading, error handling, and API calls. AppContext orchestrates them and provides cross-concern coordination (e.g., selecting a collection resets search and asset selection).

### ConfirmContext (`ConfirmContext.js`)

Hệ thống dialog dựa trên Promise:

```javascript
const { confirm, prompt, alert } = useConfirm();

const ok = await confirm('Delete this item?');         // → boolean
const name = await prompt({ message: 'New name:' });   // → string | null
await alert('Operation complete');                     // → void
```

Wraps `ConfirmDialog` component with resolve/reject pattern.

## §3 — Danh mục custom hooks

| Hook | Domain | Trạng thái chính | Tầng API |
|------|--------|-----------|-----------|
| `useAuth` | Authentication | `isAuthenticated`, `user`, `token` | `authApi` |
| `useAssets` | Asset operations | `selectedAssetId`, `assets`, `loading` | `assetApi` |
| `useAssetSelection` | Multi-select | `selectedIds`, `lastClickedId` | — (local state) |
| `useBulkOperations` | Bulk actions | — (delegates to API) | `assetApi` (bulk) |
| `useCollections` | Collections | `collections`, `selectedCollection`, `currentFolderId`, `collectionItems` | `collectionApi` |
| `useCollectionNavigation` | Breadcrumb/path | `path`, `currentFolder` | — (derived state) |
| `useSharePermissions` | Permissions | `permissions`, `myRole` | `permissionApi` |
| `useSignalR` | Real-time | `connection`, `isConnected` | SignalR hub |
| `useSmartCollections` | Smart collections | `definitions`, `loading` | `smartCollectionApi` |
| `useTags` | Tags | `tags`, `loading` | `tagApi` |
| `useUndoRedo` | Undo/redo stack | `undoStack`, `redoStack` | — (local state) |

## §4 — Mẫu luồng dữ liệu

```
User Action → Component → Hook (state update + API call) → API Service → Backend
                                                                             │
                                                                        SignalR event
                                                                             │
                                              useSignalR → callback → refreshItems()
                                                                             │
                                                                  Component re-renders
```

## §5 — Tích hợp SignalR

Các sự kiện realtime được đăng ký trong `AppContext`:

```javascript
const signalRHandlers = {
    AssetsUploaded:    () => collectionState.refreshItems(),
    AssetCreated:      () => collectionState.refreshItems(),
    AssetDeleted:      () => collectionState.refreshItems(),
    AssetsBulkDeleted: () => collectionState.refreshItems(),
    AssetsBulkMoved:   () => collectionState.refreshItems(),
    CollectionCreated: () => collectionState.refreshItems(),
    CollectionUpdated: () => collectionState.refreshItems(),
    CollectionDeleted: () => collectionState.refreshItems(),
    TagsChanged:       () => collectionState.refreshItems(),
};
useSignalR(signalRHandlers, isAuthenticated);
```

Tất cả handlers đều kích hoạt `refreshItems()` để refetch dữ liệu của collection hiện tại.

## §6 — Quyết định thiết kế

1. **Không dùng Redux** — Context + hooks đủ cho quy mô hiện tại; tránh boilerplate
2. **Composed hooks** — Mỗi hook có thể test độc lập và tái sử dụng
3. **AppContext làm orchestrator** — Điều phối xuyên các concern ở một nơi
4. **Dialog dựa trên Promise** — `useConfirm()` thay `window.confirm()` bằng async/await
5. **Tìm kiếm có debounce** — debounce 300ms trong `AppContext` qua timer `useRef`

---

> **Document End**
