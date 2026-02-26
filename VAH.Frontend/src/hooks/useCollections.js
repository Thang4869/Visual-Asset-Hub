import { useState, useEffect, useCallback, useRef } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import * as collectionsApi from '../api/collectionsApi';

/**
 * Hook encapsulating collection state and CRUD operations.
 * Syncs with React Router URL params:
 *   /collections/:collectionId
 *   /collections/:collectionId/folder/:folderId
 */
export default function useCollections() {
  const navigate = useNavigate();
  const { collectionId: urlCollectionId, folderId: urlFolderId } = useParams();

  const [collections, setCollections] = useState([]);
  const [selectedCollection, setSelectedCollection] = useState(null);
  const [collectionItems, setCollectionItems] = useState({ items: [], subCollections: [] });
  const [loading, setLoading] = useState(false);
  const [breadcrumbPath, setBreadcrumbPath] = useState([]);
  const [folderPath, setFolderPath] = useState([]);
  const [currentFolderId, setCurrentFolderId] = useState(null);

  // Track whether initial URL sync is done
  const initialSyncDone = useRef(false);

  // ------- fetch helpers -------

  const fetchCollections = useCallback(async () => {
    try {
      const data = await collectionsApi.fetchAllCollections();
      setCollections(data);
      return data;
    } catch (err) {
      console.error('Error fetching collections:', err);
      return [];
    }
  }, []);

  const fetchItems = useCallback(async (collectionId, folderId = null) => {
    setLoading(true);
    try {
      const data = await collectionsApi.fetchCollectionItems(collectionId, folderId);
      setCollectionItems({
        items: data.items || [],
        subCollections: data.subCollections || [],
      });
    } catch (err) {
      console.error('Error fetching collection items:', err);
      setCollectionItems({ items: [], subCollections: [] });
    } finally {
      setLoading(false);
    }
  }, []);

  // ------- lifecycle: fetch collections + sync from URL -------

  useEffect(() => {
    fetchCollections().then((data) => {
      if (data.length === 0) return;

      // If URL has a collectionId, select that collection
      const targetId = urlCollectionId ? parseInt(urlCollectionId, 10) : null;
      const match = targetId ? data.find((c) => c.id === targetId) : null;

      if (match) {
        setSelectedCollection(match);
        setBreadcrumbPath([match]);
        // If URL also has folderId, set it
        if (urlFolderId) {
          setCurrentFolderId(parseInt(urlFolderId, 10));
        }
      } else if (!selectedCollection) {
        // No URL match → select first collection but don't navigate (stay on home)
        if (!urlCollectionId) {
          // User is on "/" — don't auto-select, show home view
        } else {
          // Invalid collection ID in URL → go home
          navigate('/', { replace: true });
        }
      }
      initialSyncDone.current = true;
    });
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  // Sync when URL params change (browser back/forward)
  useEffect(() => {
    if (!initialSyncDone.current || collections.length === 0) return;

    const targetId = urlCollectionId ? parseInt(urlCollectionId, 10) : null;
    const targetFolderId = urlFolderId ? parseInt(urlFolderId, 10) : null;

    if (!targetId) {
      // URL is "/" → deselect collection (home view)
      if (selectedCollection) {
        setSelectedCollection(null);
        setBreadcrumbPath([]);
        setFolderPath([]);
        setCurrentFolderId(null);
      }
      return;
    }

    // Only update if different from current state
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
  }, [urlCollectionId, urlFolderId]); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    if (selectedCollection) {
      fetchItems(selectedCollection.id, currentFolderId);
    }
  }, [selectedCollection, currentFolderId, fetchItems]);

  // ------- navigation (push URL) -------

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

  const openFolder = useCallback(
    (folder) => {
      setFolderPath((prev) => [...prev, folder]);
      setCurrentFolderId(folder.id);
      if (selectedCollection) {
        navigate(`/collections/${selectedCollection.id}/folder/${folder.id}`);
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

  // ------- CRUD -------

  const handleCreateCollection = useCallback(
    async (name, parentId = null) => {
      const typeInput = prompt('Type (image/link/color/default):', 'default');
      const type = (typeInput || 'default').toLowerCase();
      try {
        await collectionsApi.createCollection({
          name,
          description: '',
          parentId,
          color: '#007bff',
          type,
          order: collections.length,
        });
        await fetchCollections();
      } catch (err) {
        console.error('Error creating collection:', err);
        alert('Lỗi khi tạo collection');
      }
    },
    [collections.length, fetchCollections],
  );

  const handleDeleteCollection = useCallback(
    async (collectionId) => {
      if (!window.confirm('Bạn có chắc chắn muốn xóa collection này?')) return;
      try {
        await collectionsApi.deleteCollection(collectionId);
        const data = await fetchCollections();
        if (selectedCollection?.id === collectionId) {
          setSelectedCollection(data[0] || null);
          if (data[0]) {
            navigate(`/collections/${data[0].id}`);
          } else {
            navigate('/');
          }
        }
      } catch (err) {
        console.error('Error deleting collection:', err);
        alert('Lỗi khi xóa collection');
      }
    },
    [fetchCollections, selectedCollection, navigate],
  );

  // ------- refresh helper -------
  const refreshItems = useCallback(() => {
    if (selectedCollection) {
      fetchItems(selectedCollection.id, currentFolderId);
    }
  }, [selectedCollection, currentFolderId, fetchItems]);

  return {
    collections,
    selectedCollection,
    collectionItems,
    loading,
    breadcrumbPath,
    folderPath,
    currentFolderId,
    // actions
    selectCollection,
    breadcrumbClick,
    navigateToCollection,
    openFolder,
    folderBreadcrumbClick,
    folderBreadcrumbRoot,
    handleCreateCollection,
    handleDeleteCollection,
    refreshItems,
    setSelectedCollection,
  };
}
