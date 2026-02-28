import { useState, useEffect, useCallback, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import * as collectionsApi from '../api/collectionsApi';
import useCollectionNavigation from './useCollectionNavigation';

/**
 * Hook encapsulating collection state and CRUD operations.
 *
 * Composes:
 *  - useCollectionNavigation → selection, breadcrumbs, folder path, URL push
 *
 * Keeps: fetch, URL param sync, CRUD (create/delete), refresh.
 */
export default function useCollections({ confirm, prompt: showPrompt, alert: showAlert } = {}) {
  const { collectionId: urlCollectionId, folderId: urlFolderId } = useParams();
  const navigate = useNavigate();

  const [collections, setCollections] = useState([]);
  const [collectionItems, setCollectionItems] = useState({ items: [], subCollections: [] });
  const [loading, setLoading] = useState(false);

  // ── Composed navigation hook ──
  const nav = useCollectionNavigation();

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

      const targetId = urlCollectionId ? parseInt(urlCollectionId, 10) : null;
      const targetFolderId = urlFolderId ? parseInt(urlFolderId, 10) : null;

      if (targetId) {
        nav.syncInitial(data, targetId, targetFolderId);
      } else if (urlCollectionId) {
        // Invalid collection ID in URL → go home
        navigate('/', { replace: true });
      }
      initialSyncDone.current = true;
    });
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  // Sync when URL params change (browser back/forward)
  useEffect(() => {
    if (!initialSyncDone.current || collections.length === 0) return;

    const targetId = urlCollectionId ? parseInt(urlCollectionId, 10) : null;
    const targetFolderId = urlFolderId ? parseInt(urlFolderId, 10) : null;

    nav.syncFromUrl(collections, targetId, targetFolderId);
  }, [urlCollectionId, urlFolderId]); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    if (nav.selectedCollection) {
      fetchItems(nav.selectedCollection.id, nav.currentFolderId);
    }
  }, [nav.selectedCollection, nav.currentFolderId, fetchItems]);

  // ------- CRUD -------

  const handleCreateCollection = useCallback(
    async (name, parentId = null) => {
      const typeInput = showPrompt
        ? await showPrompt({
            message: 'Loại collection:',
            inputType: 'select',
            defaultValue: 'default',
            selectOptions: [
              { value: 'default', label: 'Mặc định' },
              { value: 'image', label: 'Hình ảnh' },
              { value: 'link', label: 'Liên kết' },
              { value: 'color', label: 'Màu sắc' },
            ],
          })
        : 'default';
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
        if (showAlert) await showAlert('Lỗi khi tạo collection');
      }
    },
    [collections.length, fetchCollections, showPrompt, showAlert],
  );

  const handleDeleteCollection = useCallback(
    async (collectionId) => {
      const ok = confirm
        ? await confirm({ message: 'Bạn có chắc chắn muốn xóa collection này?', confirmLabel: 'Xóa', variant: 'danger' })
        : true;
      if (!ok) return;
      try {
        await collectionsApi.deleteCollection(collectionId);
        const data = await fetchCollections();
        if (nav.selectedCollection?.id === collectionId) {
          nav.setSelectedCollection(data[0] || null);
          if (data[0]) {
            navigate(`/collections/${data[0].id}`);
          } else {
            navigate('/');
          }
        }
      } catch (err) {
        console.error('Error deleting collection:', err);
        if (showAlert) await showAlert('Lỗi khi xóa collection');
      }
    },
    [fetchCollections, nav.selectedCollection, navigate, confirm, showAlert],
  );

  // ------- refresh helper -------
  const refreshItems = useCallback(() => {
    if (nav.selectedCollection) {
      fetchItems(nav.selectedCollection.id, nav.currentFolderId);
    }
  }, [nav.selectedCollection, nav.currentFolderId, fetchItems]);

  // Return merged — backward-compatible API
  return {
    collections,
    collectionItems,
    loading,
    // From useCollectionNavigation
    selectedCollection: nav.selectedCollection,
    setSelectedCollection: nav.setSelectedCollection,
    breadcrumbPath: nav.breadcrumbPath,
    folderPath: nav.folderPath,
    currentFolderId: nav.currentFolderId,
    selectCollection: nav.selectCollection,
    breadcrumbClick: nav.breadcrumbClick,
    navigateToCollection: nav.navigateToCollection,
    openFolder: nav.openFolder,
    folderBreadcrumbClick: nav.folderBreadcrumbClick,
    folderBreadcrumbRoot: nav.folderBreadcrumbRoot,
    // CRUD + refresh
    handleCreateCollection,
    handleDeleteCollection,
    refreshItems,
  };
}
