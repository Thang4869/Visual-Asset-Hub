import React, { createContext, useContext, useState, useEffect, useRef, useMemo, useCallback } from 'react';
import { useAuth } from '../hooks/useAuth';
import useCollections from '../hooks/useCollections';
import useAssets from '../hooks/useAssets';
import useTags from '../hooks/useTags';
import useSignalR from '../hooks/useSignalR';
import useUndoRedo from '../hooks/useUndoRedo';
import useSmartCollections from '../hooks/useSmartCollections';

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
  const collectionState = useCollections();
  const assetState = useAssets({
    selectedCollection: collectionState.selectedCollection,
    currentFolderId: collectionState.currentFolderId,
    collectionItems: collectionState.collectionItems,
    refreshItems: collectionState.refreshItems,
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

  // ── Keyboard shortcuts ──
  useEffect(() => {
    const handleKeyDown = (e) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 'z') {
        e.preventDefault();
        if (e.shiftKey) { undoRedo.redo(); } else { undoRedo.undo(); }
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [undoRedo.undo, undoRedo.redo]);

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
    const tagName = prompt('Nhập tên tag:');
    if (!tagName) return;
    try {
      const tag = await tagState.createTag(tagName);
      await tagState.setAssetTags(assetId, [tag.id]);
      collectionState.refreshItems();
    } catch (e) {
      console.error('Tag error:', e);
    }
  }, [tagState.createTag, tagState.setAssetTags, collectionState.refreshItems]);

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

    // Cross-concern handlers
    handleSelectCollection,
    handleOpenFolder,
    handleAddTag,
  }), [
    auth, viewMode, layoutMode, searchTerm, debouncedSearch, handleSearchChange,
    collectionState, assetState, tagState, smartState, undoRedo,
    showShareDialog, handleSelectCollection, handleOpenFolder, handleAddTag,
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
