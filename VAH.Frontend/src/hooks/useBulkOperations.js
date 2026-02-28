import { useCallback } from 'react';
import * as assetsApi from '../api/assetsApi';

/**
 * Hook encapsulating bulk operations on a set of selected assets.
 *
 * Depends on the multi-select state from useAssetSelection.
 */
export default function useBulkOperations({ selectedAssetIds, setSelectedAssetIds, setSelectedAssetId, refreshItems }) {

  const handleBulkDelete = useCallback(async () => {
    const ids = Array.from(selectedAssetIds);
    if (ids.length === 0) { alert('Chọn ít nhất một item'); return; }
    if (!confirm(`Xóa ${ids.length} item?`)) return;
    try {
      await assetsApi.bulkDelete(ids);
      setSelectedAssetIds(new Set());
      setSelectedAssetId(null);
      refreshItems();
    } catch (err) {
      console.error('Bulk delete error:', err);
      alert('Lỗi khi xóa hàng loạt');
    }
  }, [selectedAssetIds, setSelectedAssetIds, setSelectedAssetId, refreshItems]);

  const handleBulkMove = useCallback(async (targetCollectionId, targetFolderId, clearParentFolder = false) => {
    const ids = Array.from(selectedAssetIds);
    if (ids.length === 0) { alert('Chọn ít nhất một item'); return; }
    try {
      await assetsApi.bulkMove(ids, targetCollectionId, targetFolderId, clearParentFolder);
      setSelectedAssetIds(new Set());
      refreshItems();
    } catch (err) {
      console.error('Bulk move error:', err);
      alert('Lỗi khi di chuyển hàng loạt');
    }
  }, [selectedAssetIds, setSelectedAssetIds, refreshItems]);

  const handleBulkTag = useCallback(async (tagIds, remove = false) => {
    const ids = Array.from(selectedAssetIds);
    if (ids.length === 0) { alert('Chọn ít nhất một item'); return; }
    try {
      await assetsApi.bulkTag(ids, tagIds, remove);
      refreshItems();
    } catch (err) {
      console.error('Bulk tag error:', err);
      alert('Lỗi khi gán tag hàng loạt');
    }
  }, [selectedAssetIds, refreshItems]);

  const handleMoveColorsToGroup = useCallback(
    async (colorIds, targetGroupId, insertBeforeId = null) => {
      if (!colorIds || colorIds.length === 0) return;
      try {
        await assetsApi.bulkMoveGroup(colorIds, targetGroupId, insertBeforeId);
        setSelectedAssetIds(new Set());
        refreshItems();
      } catch (err) {
        console.error('Error moving colors to group:', err);
        alert('Lỗi khi di chuyển màu');
      }
    },
    [setSelectedAssetIds, refreshItems],
  );

  return {
    handleBulkDelete,
    handleBulkMove,
    handleBulkTag,
    handleMoveColorsToGroup,
  };
}
