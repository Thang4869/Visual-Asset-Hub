import { useState, useEffect, useCallback } from 'react';
import * as collectionsApi from '../api/collectionsApi';

/**
 * Hook encapsulating collection state and CRUD operations.
 */
export default function useCollections() {
  const [collections, setCollections] = useState([]);
  const [selectedCollection, setSelectedCollection] = useState(null);
  const [collectionItems, setCollectionItems] = useState({ items: [], subCollections: [] });
  const [loading, setLoading] = useState(false);
  const [breadcrumbPath, setBreadcrumbPath] = useState([]);
  const [folderPath, setFolderPath] = useState([]);
  const [currentFolderId, setCurrentFolderId] = useState(null);

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

  // ------- lifecycle -------

  useEffect(() => {
    fetchCollections().then((data) => {
      if (data.length > 0 && !selectedCollection) {
        setSelectedCollection(data[0]);
      }
    });
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    if (selectedCollection) {
      fetchItems(selectedCollection.id, currentFolderId);
    }
  }, [selectedCollection, currentFolderId, fetchItems]);

  // ------- navigation -------

  const selectCollection = useCallback((collection, path = []) => {
    setSelectedCollection(collection);
    setBreadcrumbPath(path);
    setFolderPath([]);
    setCurrentFolderId(null);
  }, []);

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
    },
    [],
  );

  const folderBreadcrumbClick = useCallback(
    (folder) => {
      const idx = folderPath.findIndex((f) => f.id === folder.id);
      if (idx >= 0) {
        setFolderPath(folderPath.slice(0, idx + 1));
        setCurrentFolderId(folder.id);
      }
    },
    [folderPath],
  );

  const folderBreadcrumbRoot = useCallback(() => {
    setFolderPath([]);
    setCurrentFolderId(null);
  }, []);

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
        }
      } catch (err) {
        console.error('Error deleting collection:', err);
        alert('Lỗi khi xóa collection');
      }
    },
    [fetchCollections, selectedCollection],
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
