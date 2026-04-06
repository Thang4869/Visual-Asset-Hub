import { useCallback } from 'react';
import * as assetsApi from '../api/assetsApi';
import useAssetSelection from './useAssetSelection';
import useBulkOperations from './useBulkOperations';

/**
 * Hook encapsulating asset-level operations.
 *
 * Composes:
 *  - useAssetSelection  → single / multi-select state
 *  - useBulkOperations  → bulk delete / move / tag
 *
 * Keeps CRUD operations (upload, create, delete, move, reorder) here.
 */
export default function useAssets({ selectedCollection, currentFolderId, collectionItems, refreshItems, confirm, prompt: showPrompt, alert: showAlert }) {
  // ── Composed hooks ──
  const selection = useAssetSelection(collectionItems);
  const bulk = useBulkOperations({
    selectedAssetIds: selection.selectedAssetIds,
    setSelectedAssetIds: selection.setSelectedAssetIds,
    setSelectedAssetId: selection.setSelectedAssetId,
    refreshItems,
    confirm,
    alert: showAlert,
  });

  // ------- Upload -------
  const handleUpload = useCallback(
    async (files) => {
      if (!selectedCollection) {
        await showAlert('Vui lòng chọn một collection trước khi upload');
        return;
      }
      try {
        await assetsApi.uploadFiles(selectedCollection.id, files, currentFolderId);
        refreshItems();
      } catch (err) {
        console.error('Error uploading files:', err);
        await showAlert('Lỗi khi upload file');
      }
    },
    [selectedCollection, currentFolderId, refreshItems, showAlert],
  );

  // ------- Folder -------
  const handleCreateFolder = useCallback(async () => {
    if (!selectedCollection) return;
    const name = await showPrompt({ message: 'Tên thư mục:', placeholder: 'Nhập tên...' });
    if (!name) return;
    try {
      await assetsApi.createFolder({
        folderName: name,
        collectionId: selectedCollection.id,
        parentFolderId: currentFolderId,
      });
      refreshItems();
    } catch (err) {
      console.error('Error creating folder:', err);
      await showAlert('Lỗi khi tạo folder');
    }
  }, [selectedCollection, currentFolderId, refreshItems, showPrompt, showAlert]);

  // ------- Link -------
  const handleCreateLink = useCallback(async () => {
    if (!selectedCollection) return;
    const name = await showPrompt({ message: 'Tên liên kết:', placeholder: 'Nhập tên...' });
    if (!name) return;
    const url = await showPrompt({ message: 'URL liên kết:', placeholder: 'https://...' });
    if (!url) return;
    try {
      await assetsApi.createLink({
        name,
        url,
        collectionId: selectedCollection.id,
        parentFolderId: currentFolderId,
      });
      refreshItems();
    } catch (err) {
      console.error('Error creating link:', err);
      await showAlert('Lỗi khi tạo link');
    }
  }, [selectedCollection, currentFolderId, refreshItems, showPrompt, showAlert]);

  // ------- Color Group -------
  const handleCreateColorGroup = useCallback(async () => {
    if (!selectedCollection) return;
    const name = await showPrompt({ message: 'Tên nhóm:', placeholder: 'Nhập tên nhóm...' });
    if (!name) return;
    try {
      await assetsApi.createColorGroup({
        groupName: name,
        collectionId: selectedCollection.id,
        parentFolderId: currentFolderId,
      });
      refreshItems();
    } catch (err) {
      console.error('Error creating color group:', err);
      await showAlert('Lỗi khi tạo color group');
    }
  }, [selectedCollection, currentFolderId, refreshItems, showPrompt, showAlert]);

  // ------- Color -------
  const handleCreateColor = useCallback(
    async (colorCode, groupId = null) => {
      if (!selectedCollection || !colorCode) return;
      try {
        await assetsApi.createColor({
          colorCode,
          collectionId: selectedCollection.id,
          groupId,
          parentFolderId: currentFolderId,
        });
        refreshItems();
      } catch (err) {
        console.error('Error creating color:', err);
        await showAlert('Lỗi khi tạo color');
      }
    },
    [selectedCollection, currentFolderId, refreshItems, showAlert],
  );

  // ------- Delete single asset -------
  const handleDeleteAsset = useCallback(
    async (assetId) => {
      if (!assetId) return;
      const ok = await confirm({ message: 'Bạn có chắc muốn xóa item này?', confirmLabel: 'Xóa', variant: 'danger' });
      if (!ok) return;
      try {
        await assetsApi.deleteAsset(assetId);
        if (selection.selectedAssetId === assetId) selection.setSelectedAssetId(null);
        refreshItems();
      } catch (err) {
        console.error('Error deleting asset:', err);
        await showAlert('Lỗi khi xóa item');
      }
    },
    [selection.selectedAssetId, refreshItems, confirm, showAlert],
  );

  // ------- Move -------
  const handleMoveAsset = useCallback(
    async (assetId, folderId) => {
      if (!selectedCollection) return;
      try {
        await assetsApi.updateAsset(assetId, { parentFolderId: folderId });
        refreshItems();
      } catch (err) {
        console.error('Error moving asset:', err);
        await showAlert('Lỗi khi di chuyển item');
      }
    },
    [selectedCollection, refreshItems, showAlert],
  );

  const handleMoveSelected = useCallback(async () => {
    if (!selection.selectedAssetId) {
      await showAlert('Chọn một item để di chuyển');
      return;
    }
    const folders = collectionItems.items.filter((i) => i.isFolder);
    if (folders.length === 0) {
      await showAlert('Không có folder trong thư mục hiện tại');
      return;
    }
    const targetName = await showPrompt({ message: 'Nhập tên folder (hoặc gõ "root" để đưa ra ngoài):', placeholder: 'Tên folder...' });
    if (!targetName) return;

    if (targetName.toLowerCase() === 'root') {
      try {
        await assetsApi.updateAsset(selection.selectedAssetId, { clearParentFolder: true });
        refreshItems();
      } catch (err) {
        console.error('Error moving asset to root:', err);
        await showAlert('Lỗi khi di chuyển item');
      }
      return;
    }

    const folder = folders.find((f) => f.fileName.toLowerCase() === targetName.toLowerCase());
    if (!folder) {
      await showAlert('Không tìm thấy folder tên đó');
      return;
    }
    handleMoveAsset(selection.selectedAssetId, folder.id);
  }, [selection.selectedAssetId, collectionItems, refreshItems, handleMoveAsset, showPrompt, showAlert]);

  // ------- Reorder -------
  const handleReorderAssets = useCallback(
    async (assetIds) => {
      if (!selectedCollection) return;
      try {
        await assetsApi.reorderAssets(assetIds);
        refreshItems();
      } catch (err) {
        console.error('Error reordering assets:', err);
        await showAlert('Lỗi khi sắp xếp item');
      }
    },
    [selectedCollection, refreshItems, showAlert],
  );

  // Return merged: selection + bulk + CRUD ops (backward-compatible API)
  return {
    // From useAssetSelection
    ...selection,
    // From useBulkOperations
    ...bulk,
    // CRUD operations
    handleUpload,
    handleCreateFolder,
    handleCreateLink,
    handleCreateColorGroup,
    handleCreateColor,
    handleDeleteAsset,
    handleMoveAsset,
    handleMoveSelected,
    handleReorderAssets,
  };
}
