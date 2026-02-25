import { useState, useCallback } from 'react';
import * as assetsApi from '../api/assetsApi';

/**
 * Hook encapsulating asset-level operations.
 */
export default function useAssets({ selectedCollection, currentFolderId, collectionItems, refreshItems }) {
  const [selectedAssetId, setSelectedAssetId] = useState(null);

  // ------- Upload -------
  const handleUpload = useCallback(
    async (files) => {
      if (!selectedCollection) {
        alert('Vui lòng chọn một collection trước khi upload');
        return;
      }
      try {
        await assetsApi.uploadFiles(selectedCollection.id, files, currentFolderId);
        refreshItems();
      } catch (err) {
        console.error('Error uploading files:', err);
        alert('Lỗi khi upload file');
      }
    },
    [selectedCollection, currentFolderId, refreshItems],
  );

  // ------- Folder -------
  const handleCreateFolder = useCallback(async () => {
    if (!selectedCollection) return;
    const name = prompt('Folder name:');
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
      alert('Lỗi khi tạo folder');
    }
  }, [selectedCollection, currentFolderId, refreshItems]);

  // ------- Link -------
  const handleCreateLink = useCallback(async () => {
    if (!selectedCollection) return;
    const name = prompt('Link name:');
    if (!name) return;
    const url = prompt('Link URL:');
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
      alert('Lỗi khi tạo link');
    }
  }, [selectedCollection, currentFolderId, refreshItems]);

  // ------- Color Group -------
  const handleCreateColorGroup = useCallback(async () => {
    if (!selectedCollection) return;
    const name = prompt('Group name:');
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
      alert('Lỗi khi tạo color group');
    }
  }, [selectedCollection, currentFolderId, refreshItems]);

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
        alert('Lỗi khi tạo color');
      }
    },
    [selectedCollection, currentFolderId, refreshItems],
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
        alert('Lỗi khi di chuyển item');
      }
    },
    [selectedCollection, refreshItems],
  );

  const handleMoveSelected = useCallback(async () => {
    if (!selectedAssetId) {
      alert('Chọn một item để di chuyển');
      return;
    }
    const folders = collectionItems.items.filter((i) => i.isFolder);
    if (folders.length === 0) {
      alert('Không có folder trong thư mục hiện tại');
      return;
    }
    const targetName = prompt('Nhập tên folder (hoặc gõ "root" để đưa ra ngoài):');
    if (!targetName) return;

    if (targetName.toLowerCase() === 'root') {
      try {
        await assetsApi.updateAsset(selectedAssetId, { clearParentFolder: true });
        refreshItems();
      } catch (err) {
        console.error('Error moving asset to root:', err);
        alert('Lỗi khi di chuyển item');
      }
      return;
    }

    const folder = folders.find((f) => f.fileName.toLowerCase() === targetName.toLowerCase());
    if (!folder) {
      alert('Không tìm thấy folder tên đó');
      return;
    }
    handleMoveAsset(selectedAssetId, folder.id);
  }, [selectedAssetId, collectionItems, refreshItems, handleMoveAsset]);

  // ------- Reorder -------
  const handleReorderAssets = useCallback(
    async (assetIds) => {
      if (!selectedCollection) return;
      try {
        await assetsApi.reorderAssets(assetIds);
        refreshItems();
      } catch (err) {
        console.error('Error reordering assets:', err);
        alert('Lỗi khi sắp xếp item');
      }
    },
    [selectedCollection, refreshItems],
  );

  // ------- derived -------
  const selectedAsset = selectedAssetId
    ? collectionItems.items.find((a) => a.id === selectedAssetId)
    : null;

  return {
    selectedAssetId,
    setSelectedAssetId,
    selectedAsset,
    handleUpload,
    handleCreateFolder,
    handleCreateLink,
    handleCreateColorGroup,
    handleCreateColor,
    handleMoveAsset,
    handleMoveSelected,
    handleReorderAssets,
  };
}
