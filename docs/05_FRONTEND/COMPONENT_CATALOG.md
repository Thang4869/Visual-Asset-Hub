# DANH MỤC COMPONENT — React Components phía frontend

> **Last Updated**: 2026-04-06
> **Ghi chú**: Danh sách component đã đối chiếu với `VAH.Frontend/src/components` — tên file chỉ mang tính minh họa; giữ component nhỏ và thiên về hooks.
---
## §1 — Danh mục component

### §1.1 — Component bố cục

| Component | File | Mục đích |
|-----------|------|---------|
| `AppHeader` | `AppHeader.jsx` | Top navigation bar — search, view mode toggle, user menu |
| `AppSidebar` | `AppSidebar.jsx` | Left sidebar — collection tree, smart collections, actions |

### §1.2 — Component hiển thị asset

| Component | File | Mục đích |
|-----------|------|---------|
| `AssetGrid` | `AssetGrid.jsx` | Grid/list view of assets in a collection |
| `AssetDisplayer` | `AssetDisplayer.jsx` | Single asset card rendering (thumbnail, name, type icon) |
| `DraggableAssetCanvas` | `DraggableAssetCanvas.jsx` | Canvas layout — free-position drag-and-drop for assets |
| `ColorBoard` | `ColorBoard.jsx` | Color palette display for color/color-group assets |
| `DetailsPanel` | `DetailsPanel.jsx` | Right panel — selected asset metadata, tags, actions |

### §1.3 — Component điều hướng

| Component | File | Mục đích |
|-----------|------|---------|
| `CollectionBrowser` | `CollectionBrowser.jsx` | Main content area — routes between collections |
| `CollectionTree` | `CollectionTree.jsx` | Hierarchical tree of collections in sidebar |
| `TreeViewPanel` | `TreeViewPanel.jsx` | Folder tree within a collection |
| `SearchBar` | `SearchBar.jsx` | Search input with debounce |

### §1.4 — Component hộp thoại

| Component | File | Mục đích |
|-----------|------|---------|
| `ConfirmDialog` | `ConfirmDialog.jsx` | Promise-based confirm/prompt/alert modal |
| `ShareDialog` | `ShareDialog.jsx` | Collection sharing — grant/revoke permissions |
| `ContextMenu` | `ContextMenu.jsx` | Right-click context menu for assets/folders |

### §1.5 — Component tiện ích

| Component | File | Mục đích |
|-----------|------|---------|
| `UploadArea` | `UploadArea.jsx` | Drag-and-drop file upload zone |
| `LoginPage` | `LoginPage.jsx` | Authentication page (login/register) |
| `ErrorBoundary` | `ErrorBoundary.jsx` | React error boundary — catches render errors |

## §2 — Phân cấp component

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

## §3 — Quy tắc thiết kế component

1. **Không dùng class components** — tất cả component là functional với hooks
2. **Không gọi API trực tiếp** — component chỉ dùng hooks, còn hooks gọi API services
3. **Dựa trên props** — component nhận dữ liệu qua props hoặc context, không qua global state
4. **Single responsibility** — mỗi component xử lý một concern UI
5. **Error boundaries** — `ErrorBoundary` bọc layout chính

## §4 — Chi tiết component chính

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
