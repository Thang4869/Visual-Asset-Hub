# STATE MANAGEMENT — Context & Hooks Architecture

> **Last Updated**: 2026-03-02

---

## §1 — Architecture Overview

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

Central context that composes all domain hooks into a single provider. Eliminates prop-drilling across the component tree.

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

Promise-based dialog system:

```javascript
const { confirm, prompt, alert } = useConfirm();

const ok = await confirm('Delete this item?');         // → boolean
const name = await prompt({ message: 'New name:' });   // → string | null
await alert('Operation complete');                     // → void
```

Wraps `ConfirmDialog` component with resolve/reject pattern.

## §3 — Custom Hooks Inventory

| Hook | Domain | Key State | API Layer |
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

## §4 — Data Flow Pattern

```
User Action → Component → Hook (state update + API call) → API Service → Backend
                                                                             │
                                                                        SignalR event
                                                                             │
                                              useSignalR → callback → refreshItems()
                                                                             │
                                                                  Component re-renders
```

## §5 — SignalR Integration

Real-time events are registered in `AppContext`:

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

All handlers trigger a `refreshItems()` which refetches the current collection's data.

## §6 — Design Decisions

1. **No Redux** — Context + hooks sufficient for current scale; avoids boilerplate
2. **Composed hooks** — Each hook is independently testable and reusable
3. **AppContext as orchestrator** — Cross-concern coordination in one place
4. **Promise-based dialogs** — `useConfirm()` replaces `window.confirm()` with async/await
5. **Debounced search** — 300ms debounce in `AppContext` via `useRef` timer

---

> **Document End**
