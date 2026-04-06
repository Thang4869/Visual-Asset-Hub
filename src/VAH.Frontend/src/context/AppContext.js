import React, { createContext, useContext, useState, useEffect, useRef, useMemo, useCallback } from 'react';
import { useAuth } from '../hooks/useAuth';
import useCollections from '../hooks/useCollections';
import useAssets from '../hooks/useAssets';
import useTags from '../hooks/useTags';
import useSignalR from '../hooks/useSignalR';
import useUndoRedo from '../hooks/useUndoRedo';
import useSmartCollections from '../hooks/useSmartCollections';
import * as assetsApi from '../api/assetsApi';
import * as collectionsApi from '../api/collectionsApi';
import { useConfirm } from './ConfirmContext';

/**
 * AppContext — centralised state management for the main application layout.
 *
 * Composes all domain hooks and exposes a single context so child components
 * can consume slices of state without prop-drilling through AppLayout.
 *
 * Usage:
 *   <AppProvider>
 *     <SomeComponent />   // inside: const { collections, selectedAsset } = useAppContext();
 *   </AppProvider>
 */
const AppContext = createContext(null);

export function AppProvider({ children }) {
  const auth = useAuth();
  const { isAuthenticated } = auth;
  const { confirm, prompt: showPrompt, alert: showAlert } = useConfirm();

  // ── View state ──
  const [viewMode, setViewMode] = useState('browser');
  const [layoutMode, setLayoutMode] = useState('grid');
  const [searchTerm, setSearchTerm] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const debounceRef = useRef(null);

  const handleSearchChange = useCallback((value) => {
    setSearchTerm(value);
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => setDebouncedSearch(value), 300);
  }, []);

  useEffect(() => {
    return () => { if (debounceRef.current) clearTimeout(debounceRef.current); };
  }, []);

  // ── Domain hooks ──
  const collectionState = useCollections({ confirm, prompt: showPrompt, alert: showAlert });
  const assetState = useAssets({
    selectedCollection: collectionState.selectedCollection,
    currentFolderId: collectionState.currentFolderId,
    collectionItems: collectionState.collectionItems,
    refreshItems: collectionState.refreshItems,
    confirm,
    prompt: showPrompt,
    alert: showAlert,
  });
  const tagState = useTags();
  const smartState = useSmartCollections(isAuthenticated);

  // ── Real-time sync ──
  const signalRHandlers = useMemo(() => ({
    AssetsUploaded: () => collectionState.refreshItems(),
    AssetCreated: () => collectionState.refreshItems(),
    AssetDeleted: () => collectionState.refreshItems(),
    AssetsBulkDeleted: () => collectionState.refreshItems(),
    AssetsBulkMoved: () => collectionState.refreshItems(),
    CollectionCreated: () => collectionState.refreshItems(),
    CollectionUpdated: () => collectionState.refreshItems(),
    CollectionDeleted: () => collectionState.refreshItems(),
    TagsChanged: () => collectionState.refreshItems(),
  }), [collectionState.refreshItems]);

  useSignalR(signalRHandlers, isAuthenticated);

  // ── Undo/Redo ──
  const undoRedo = useUndoRedo();

  // ── Share dialog ──
  const [showShareDialog, setShowShareDialog] = useState(false);

  // ── Clipboard & Pin state ──
  const [clipboard, setClipboard] = useState(null); // { item, type, action: 'copy'|'cut' }
  const [pinnedItems, setPinnedItems] = useState([]);
  const [selectedFolderIds, setSelectedFolderIds] = useState(new Set());
  const [treeViewCollapsed, setTreeViewCollapsed] = useState(false);

  // ── Convenience handlers (cross-concern coordination) ──
  const handleSelectCollection = useCallback((collection, path = []) => {
    collectionState.selectCollection(collection, path);
    assetState.setSelectedAssetId(null);
    setSearchTerm('');
    setDebouncedSearch('');
  }, [collectionState.selectCollection, assetState.setSelectedAssetId]);

  const handleOpenFolder = useCallback((folder) => {
    collectionState.openFolder(folder);
    assetState.setSelectedAssetId(null);
  }, [collectionState.openFolder, assetState.setSelectedAssetId]);

  const handleAddTag = useCallback(async (assetId) => {
    const tagName = await showPrompt({ message: 'Nhập tên tag:', placeholder: 'Tag name...' });
    if (!tagName) return;
    try {
      const tag = await tagState.createTag(tagName);
      await tagState.setAssetTags(assetId, [tag.id]);
      collectionState.refreshItems();
    } catch (e) {
      console.error('Tag error:', e);
    }
  }, [tagState.createTag, tagState.setAssetTags, collectionState.refreshItems, showPrompt]);

  // ── Folder selection handler ──
  const handleSelectFolderItem = useCallback((folderId, event) => {
    if (event?.ctrlKey || event?.metaKey) {
      setSelectedFolderIds(prev => {
        const next = new Set(prev);
        if (next.has(folderId)) next.delete(folderId);
        else next.add(folderId);
        return next;
      });
    } else {
      setSelectedFolderIds(new Set([folderId]));
    }
  }, []);

  // ── Delete folder (asset) handler ──
  const handleDeleteFolder = useCallback(async (folderId) => {
    const ok = await confirm({ message: 'Bạn có chắc muốn xóa thư mục này?', confirmLabel: 'Xóa', variant: 'danger' });
    if (!ok) return;
    try {
      await assetsApi.deleteAsset(folderId);
      setSelectedFolderIds(prev => {
        const next = new Set(prev);
        next.delete(folderId);
        return next;
      });
      collectionState.refreshItems();
    } catch (e) {
      console.error('Delete folder error:', e);
    }
  }, [collectionState.refreshItems, confirm]);

  // ── Delete single asset handler ──
  const handleDeleteAsset = useCallback(async (assetId) => {
    const ok = await confirm({ message: 'Bạn có chắc muốn xóa item này?', confirmLabel: 'Xóa', variant: 'danger' });
    if (!ok) return;
    try {
      await assetsApi.deleteAsset(assetId);
      assetState.setSelectedAssetId(null);
      collectionState.refreshItems();
    } catch (e) {
      console.error('Delete asset error:', e);
      await showAlert({ message: 'Không thể xóa item. Bạn có thể không có quyền chỉnh sửa.' });
    }
  }, [assetState.setSelectedAssetId, collectionState.refreshItems, confirm]);

  // ── Rename asset ──
  const handleRenameAsset = useCallback(async (asset) => {
    const newName = await showPrompt({ message: 'Nhập tên mới:', defaultValue: asset.fileName });
    if (!newName || newName === asset.fileName) return;
    try {
      await assetsApi.updateAsset(asset.id, { fileName: newName });
      collectionState.refreshItems();
    } catch (e) {
      console.error('Rename error:', e);
    }
  }, [collectionState.refreshItems, showPrompt]);

  // ── Rename collection ──
  const handleRenameCollection = useCallback(async (collection) => {
    const newName = await showPrompt({ message: 'Nhập tên mới:', defaultValue: collection.name });
    if (!newName || newName === collection.name) return;
    try {
      await collectionsApi.updateCollection(collection.id, { name: newName });
      // Refresh collections list and current items
      collectionState.refreshItems();
      // Update sidebar collections
      const data = await collectionState.fetchCollections();
      // If renamed the selected collection, update its reference
      if (collectionState.selectedCollection?.id === collection.id) {
        const updated = (data || []).find(c => c.id === collection.id);
        if (updated) collectionState.setSelectedCollection(updated);
      }
    } catch (e) {
      console.error('Rename collection error:', e);
      await showAlert({ message: 'Lỗi khi đổi tên collection' });
    }
  }, [showPrompt, showAlert, collectionState.refreshItems, collectionState.fetchCollections, collectionState.selectedCollection, collectionState.setSelectedCollection]);

  // ── Clipboard handlers ──
  const handleCopy = useCallback((item, type) => {
    setClipboard({ item, type, action: 'copy' });
  }, []);

  const handleCut = useCallback((item, type) => {
    setClipboard({ item, type, action: 'cut' });
  }, []);

  const handlePaste = useCallback(async (targetItem, targetType) => {
    if (!clipboard) return;
    try {
      if (clipboard.action === 'cut') {
        // Cut = move to target
        const update = {};
        if (targetType === 'folder') {
          update.parentFolderId = targetItem.id;
        } else if (targetType === 'color-group') {
          update.groupId = targetItem.id;
        } else if (targetType === 'area') {
          // Paste at root level — clear parentFolderId/groupId
          update.clearParentFolder = true;
          update.clearGroup = true;
        }
        if (Object.keys(update).length > 0) {
          await assetsApi.updateAsset(clipboard.item.id, update);
        }
        setClipboard(null);
      } else {
        // Copy = duplicate
        const targetFolderId = targetType === 'folder' ? targetItem.id : null;
        await assetsApi.duplicateAsset(clipboard.item.id, targetFolderId);
      }
      collectionState.refreshItems();
    } catch (e) {
      console.error('Paste error:', e);
    }
  }, [clipboard, collectionState.refreshItems]);

  // ── Pin handler (toggle pinned in local storage + state) ──
  const handlePinItem = useCallback((item, type) => {
    setPinnedItems(prev => {
      const key = `${type}-${item.id}`;
      const exists = prev.find(p => `${p.type}-${p.item.id}` === key);
      let next;
      if (exists) {
        next = prev.filter(p => `${p.type}-${p.item.id}` !== key);
      } else {
        next = [...prev, { item, type }];
      }
      // Persist to localStorage
      try { localStorage.setItem('vah_pinned', JSON.stringify(next)); } catch {}
      return next;
    });
  }, []);

  // Restore pinned items from localStorage on mount
  useEffect(() => {
    try {
      const saved = localStorage.getItem('vah_pinned');
      if (saved) setPinnedItems(JSON.parse(saved));
    } catch {}
  }, []);

  // ── Tree view toggle ──
  const handleToggleTreeView = useCallback(() => {
    setTreeViewCollapsed(prev => !prev);
  }, []);

  // ── View detail handler (open details panel) ──
  const handleViewDetail = useCallback((item) => {
    if (item?.id) {
      assetState.setSelectedAssetId(item.id);
    }
  }, [assetState.setSelectedAssetId]);

  // ── Ungroup color (remove from group) ──
  const handleUngroupColor = useCallback(async (item) => {
    try {
      await assetsApi.updateAsset(item.id, { clearGroup: true });
      collectionState.refreshItems();
    } catch (e) {
      console.error('Ungroup error:', e);
    }
  }, [collectionState.refreshItems]);

  // ── Navigate to pinned item's location ──
  const handleNavigateToPinned = useCallback((item, type) => {
    if (type === 'collection') {
      // Navigate to that collection
      const col = collectionState.collections.find(c => c.id === item.id);
      if (col) handleSelectCollection(col, [col]);
    } else if (type === 'folder') {
      // Open the folder — find its collection first
      const col = collectionState.collections.find(c => c.id === item.collectionId)
        || collectionState.selectedCollection;
      if (col) {
        handleSelectCollection(col, [col]);
        collectionState.openFolder(item, col);
      }
    } else {
      // It's an asset (image, link, color) — navigate to its collection + folder, then select it
      const col = collectionState.collections.find(c => c.id === item.collectionId)
        || collectionState.selectedCollection;
      if (col) {
        handleSelectCollection(col, [col]);
        if (item.parentFolderId) {
          collectionState.openFolder({
            id: item.parentFolderId,
            fileName: item.parentFolderName || '...',
            collectionId: col.id,
          }, col);
        }
        assetState.setSelectedAssetId(item.id);
      }
    }
  }, [collectionState.collections, collectionState.selectedCollection, collectionState.openFolder, handleSelectCollection, assetState.setSelectedAssetId]);

  // ── Keyboard shortcuts ──
  useEffect(() => {
    const handleKeyDown = (e) => {
      // Skip if user is typing in an input/textarea
      const tag = document.activeElement?.tagName;
      if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT') return;

      // Ctrl+Z / Ctrl+Shift+Z — undo/redo
      if ((e.ctrlKey || e.metaKey) && e.key === 'z') {
        e.preventDefault();
        if (e.shiftKey) { undoRedo.redo(); } else { undoRedo.undo(); }
        return;
      }

      // Delete / Backspace — delete selected asset
      if (e.key === 'Delete' || e.key === 'Backspace') {
        if (assetState.selectedAssetId) {
          e.preventDefault();
          handleDeleteAsset(assetState.selectedAssetId);
        }
        return;
      }

      // F2 — rename selected asset
      if (e.key === 'F2') {
        if (assetState.selectedAssetId) {
          e.preventDefault();
          const asset = collectionState.collectionItems?.items?.find(a => a.id === assetState.selectedAssetId);
          if (asset) handleRenameAsset(asset);
        }
        return;
      }

      // Ctrl+C / Ctrl+X / Ctrl+V — clipboard operations
      if (!(e.ctrlKey || e.metaKey)) return;

      if (e.key === 'c') {
        if (assetState.selectedAssetId) {
          e.preventDefault();
          const asset = collectionState.collectionItems?.items?.find(a => a.id === assetState.selectedAssetId);
          if (asset) handleCopy(asset, asset.isFolder ? 'folder' : asset.contentType);
        }
        return;
      }

      if (e.key === 'x') {
        if (assetState.selectedAssetId) {
          e.preventDefault();
          const asset = collectionState.collectionItems?.items?.find(a => a.id === assetState.selectedAssetId);
          if (asset) handleCut(asset, asset.isFolder ? 'folder' : asset.contentType);
        }
        return;
      }

      if (e.key === 'v' && clipboard) {
        e.preventDefault();
        // Paste into current folder or collection root
        const target = collectionState.currentFolderId
          ? { id: collectionState.currentFolderId }
          : { id: collectionState.selectedCollection?.id };
        const targetType = collectionState.currentFolderId ? 'folder' : 'area';
        handlePaste(target, targetType);
        return;
      }

      // Ctrl+A — select all
      if (e.key === 'a' && collectionState.selectedCollection) {
        e.preventDefault();
        assetState.selectAllAssets();
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [
    undoRedo.undo, undoRedo.redo,
    assetState.selectedAssetId, assetState.selectAllAssets,
    collectionState.collectionItems, collectionState.currentFolderId, collectionState.selectedCollection,
    clipboard, handleCopy, handleCut, handlePaste, handleDeleteAsset, handleRenameAsset,
  ]);

  // ── Context value (memoised to avoid unnecessary re-renders) ──
  const value = useMemo(() => ({
    // Auth (re-exported for convenience)
    ...auth,

    // View state
    viewMode,
    setViewMode,
    layoutMode,
    setLayoutMode,
    searchTerm,
    debouncedSearch,
    handleSearchChange,

    // Collections
    ...collectionState,

    // Assets
    ...assetState,

    // Tags
    ...tagState,

    // Smart collections
    ...smartState,

    // Undo/Redo
    ...undoRedo,

    // Share dialog
    showShareDialog,
    setShowShareDialog,

    // Clipboard & Pin
    clipboard,
    pinnedItems,
    selectedFolderIds,
    setSelectedFolderIds,
    treeViewCollapsed,

    // Cross-concern handlers
    handleSelectCollection,
    handleOpenFolder,
    handleAddTag,
    handleSelectFolderItem,
    handleDeleteFolder,
    handleDeleteAsset,
    handleRenameAsset,
    handleRenameCollection,
    handleCopy,
    handleCut,
    handlePaste,
    handlePinItem,
    handleToggleTreeView,
    handleViewDetail,
    handleUngroupColor,
    handleNavigateToPinned,
  }), [
    auth, viewMode, layoutMode, searchTerm, debouncedSearch, handleSearchChange,
    collectionState, assetState, tagState, smartState, undoRedo,
    showShareDialog, clipboard, pinnedItems, selectedFolderIds, treeViewCollapsed,
    handleSelectCollection, handleOpenFolder, handleAddTag,
    handleSelectFolderItem, handleDeleteFolder, handleDeleteAsset,
    handleRenameAsset, handleRenameCollection,
    handleCopy, handleCut, handlePaste, handlePinItem, handleToggleTreeView,
    handleViewDetail, handleUngroupColor, handleNavigateToPinned,
  ]);

  return React.createElement(AppContext.Provider, { value }, children);
}

/**
 * Convenience hook — access the full application context.
 *
 * For components that only need a slice, destructure:
 *   const { selectedCollection, layoutMode } = useAppContext();
 */
export function useAppContext() {
  const ctx = useContext(AppContext);
  if (!ctx) throw new Error('useAppContext must be used within <AppProvider>');
  return ctx;
}
