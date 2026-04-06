import { useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';

/**
 * Hook encapsulating collection navigation state and URL management.
 *
 * Manages: selectedCollection, breadcrumbs, folder path, URL sync (push).
 * Does NOT own URL-param reading (that stays in useCollections orchestrator).
 */
export default function useCollectionNavigation() {
  const navigate = useNavigate();

  const [selectedCollection, setSelectedCollection] = useState(null);
  const [breadcrumbPath, setBreadcrumbPath] = useState([]);
  const [folderPath, setFolderPath] = useState([]);
  const [currentFolderId, setCurrentFolderId] = useState(null);

  // ── Collection-level navigation ──

  const selectCollection = useCallback((collection, path = []) => {
    setSelectedCollection(collection);
    setBreadcrumbPath(path);
    setFolderPath([]);
    setCurrentFolderId(null);
    if (collection) {
      navigate(`/collections/${collection.id}`);
    } else {
      navigate('/');
    }
  }, [navigate]);

  const breadcrumbClick = useCallback(
    (collection) => {
      const idx = breadcrumbPath.findIndex((c) => c.id === collection.id);
      selectCollection(collection, breadcrumbPath.slice(0, idx + 1));
    },
    [breadcrumbPath, selectCollection],
  );

  const navigateToCollection = useCallback(
    (collection) => {
      selectCollection(collection, [...breadcrumbPath, collection]);
    },
    [breadcrumbPath, selectCollection],
  );

  // ── Folder-level navigation ──

  const openFolder = useCallback(
    (folder, collectionOverride = null) => {
      setFolderPath((prev) => [...prev, folder]);
      setCurrentFolderId(folder.id);
      const targetCollectionId = collectionOverride?.id || selectedCollection?.id || folder?.collectionId;
      if (targetCollectionId) {
        navigate(`/collections/${targetCollectionId}/folder/${folder.id}`);
      }
    },
    [selectedCollection, navigate],
  );

  const folderBreadcrumbClick = useCallback(
    (folder) => {
      const idx = folderPath.findIndex((f) => f.id === folder.id);
      if (idx >= 0) {
        setFolderPath(folderPath.slice(0, idx + 1));
        setCurrentFolderId(folder.id);
        if (selectedCollection) {
          navigate(`/collections/${selectedCollection.id}/folder/${folder.id}`);
        }
      }
    },
    [folderPath, selectedCollection, navigate],
  );

  const folderBreadcrumbRoot = useCallback(() => {
    setFolderPath([]);
    setCurrentFolderId(null);
    if (selectedCollection) {
      navigate(`/collections/${selectedCollection.id}`);
    }
  }, [selectedCollection, navigate]);

  // ── URL sync helpers (called by parent orchestrator) ──

  /** Sync state when URL params change (browser back/forward). */
  const syncFromUrl = useCallback((collections, targetId, targetFolderId) => {
    if (!targetId) {
      if (selectedCollection) {
        setSelectedCollection(null);
        setBreadcrumbPath([]);
        setFolderPath([]);
        setCurrentFolderId(null);
      }
      return;
    }

    if (selectedCollection?.id !== targetId) {
      const match = collections.find((c) => c.id === targetId);
      if (match) {
        setSelectedCollection(match);
        setBreadcrumbPath([match]);
        setFolderPath([]);
        setCurrentFolderId(targetFolderId);
      }
    } else if ((currentFolderId || null) !== (targetFolderId || null)) {
      if (targetFolderId) {
        setCurrentFolderId(targetFolderId);
      } else {
        setFolderPath([]);
        setCurrentFolderId(null);
      }
    }
  }, [selectedCollection, currentFolderId]);

  /** Set initial state from URL (first load). */
  const syncInitial = useCallback((collections, targetId, targetFolderId) => {
    const match = targetId ? collections.find((c) => c.id === targetId) : null;
    if (match) {
      setSelectedCollection(match);
      setBreadcrumbPath([match]);
      if (targetFolderId) {
        setCurrentFolderId(targetFolderId);
      }
    }
  }, []);

  return {
    selectedCollection,
    setSelectedCollection,
    breadcrumbPath,
    folderPath,
    currentFolderId,
    // Navigation actions
    selectCollection,
    breadcrumbClick,
    navigateToCollection,
    openFolder,
    folderBreadcrumbClick,
    folderBreadcrumbRoot,
    // URL sync helpers
    syncFromUrl,
    syncInitial,
  };
}
