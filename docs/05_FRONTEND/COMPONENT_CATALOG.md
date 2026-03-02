# COMPONENT CATALOG — Frontend React Components

> **Last Updated**: 2026-03-02

---

## §1 — Component Inventory

### 1.1 Layout Components

| Component | File | Purpose |
|-----------|------|---------|
| `AppHeader` | `AppHeader.jsx` | Top navigation bar — search, view mode toggle, user menu |
| `AppSidebar` | `AppSidebar.jsx` | Left sidebar — collection tree, smart collections, actions |

### 1.2 Asset Display Components

| Component | File | Purpose |
|-----------|------|---------|
| `AssetGrid` | `AssetGrid.jsx` | Grid/list view of assets in a collection |
| `AssetDisplayer` | `AssetDisplayer.jsx` | Single asset card rendering (thumbnail, name, type icon) |
| `DraggableAssetCanvas` | `DraggableAssetCanvas.jsx` | Canvas layout — free-position drag-and-drop for assets |
| `ColorBoard` | `ColorBoard.jsx` | Color palette display for color/color-group assets |
| `DetailsPanel` | `DetailsPanel.jsx` | Right panel — selected asset metadata, tags, actions |

### 1.3 Navigation Components

| Component | File | Purpose |
|-----------|------|---------|
| `CollectionBrowser` | `CollectionBrowser.jsx` | Main content area — routes between collections |
| `CollectionTree` | `CollectionTree.jsx` | Hierarchical tree of collections in sidebar |
| `TreeViewPanel` | `TreeViewPanel.jsx` | Folder tree within a collection |
| `SearchBar` | `SearchBar.jsx` | Search input with debounce |

### 1.4 Dialog Components

| Component | File | Purpose |
|-----------|------|---------|
| `ConfirmDialog` | `ConfirmDialog.jsx` | Promise-based confirm/prompt/alert modal |
| `ShareDialog` | `ShareDialog.jsx` | Collection sharing — grant/revoke permissions |
| `ContextMenu` | `ContextMenu.jsx` | Right-click context menu for assets/folders |

### 1.5 Utility Components

| Component | File | Purpose |
|-----------|------|---------|
| `UploadArea` | `UploadArea.jsx` | Drag-and-drop file upload zone |
| `LoginPage` | `LoginPage.jsx` | Authentication page (login/register) |
| `ErrorBoundary` | `ErrorBoundary.jsx` | React error boundary — catches render errors |

## §2 — Component Hierarchy

```
App
├── ConfirmProvider (context)
│   └── AppProvider (context)
│       ├── LoginPage           (when !isAuthenticated)
│       └── AppLayout           (when isAuthenticated)
│           ├── AppHeader
│           │   └── SearchBar
│           ├── AppSidebar
│           │   ├── CollectionTree
│           │   └── SmartCollections (inline)
│           ├── CollectionBrowser
│           │   ├── AssetGrid
│           │   │   └── AssetDisplayer (×N)
│           │   ├── DraggableAssetCanvas
│           │   │   └── AssetDisplayer (×N)
│           │   ├── ColorBoard
│           │   ├── TreeViewPanel
│           │   └── UploadArea
│           ├── DetailsPanel
│           ├── ContextMenu
│           ├── ShareDialog
│           ├── ConfirmDialog
│           └── ErrorBoundary
```

## §3 — Component Design Rules

1. **No class components** — all components are functional with hooks
2. **No local API calls** — components consume hooks which call API services
3. **Props-driven** — components receive data via props or context, not global state
4. **Single responsibility** — each component handles one UI concern
5. **Error boundaries** — `ErrorBoundary` wraps the main layout

## §4 — Key Component Details

### AssetGrid
- Renders assets in grid or list mode based on `layoutMode`
- Supports multi-select via `useAssetSelection` hook
- Handles keyboard shortcuts (Ctrl+A, Delete, etc.)
- Integrates with `ContextMenu` for right-click actions

### DraggableAssetCanvas
- Free-form canvas with drag-to-position
- Assets stored with `(PositionX, PositionY)` coordinates
- Updates position via `UpdateAssetPositionCommand` on drag end
- Only active when collection `LayoutType === 'Canvas'`

### CollectionTree
- Recursive tree rendering of `Collection.Children`
- Drag-and-drop for reordering collections
- Inline rename and color picker
- Expandable/collapsible nodes

### ConfirmDialog
- Promise-based API via `useConfirm()` hook
- Modes: confirm (boolean), prompt (string input), alert (acknowledgement)
- Variant styling: danger, info, warning

---

> **Document End**
