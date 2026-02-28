import { useCallback } from 'react';
import * as assetsApi from '../api/assetsApi';

/**
 * Hook encapsulating bulk operations on a set of selected assets.
 *
 * Depends on the multi-select state from useAssetSelection.
 */
export default function useBulkOperations({ selectedAssetIds, setSelectedAssetIds, setSelectedAssetId, refreshItems, confirm: confirmFn, alert: alertFn }) {

  const showAlert = async (msg) => { if (alertFn) await alertFn(msg); };

  const handleBulkDelete = useCallback(async () => {
    const ids = Array.from(selectedAssetIds);
    if (ids.length === 0) { await showAlert('Chọn ít nhất một item'); return; }
    const ok = confirmFn ? await confirmFn({ message: `Xóa ${ids.length} item?`, confirmLabel: 'Xóa', variant: 'danger' }) : true;
    if (!ok) return;
    try {
      await assetsApi.bulkDelete(ids);
      setSelectedAssetIds(new Set());
      setSelectedAssetId(null);
      refreshItems();
    } catch (err) {
      console.error('Bulk delete error:', err);
      await showAlert('Lỗi khi xóa hàng loạt');
    }
  }, [selectedAssetIds, setSelectedAssetIds, setSelectedAssetId, refreshItems, confirmFn, alertFn]);

  const handleBulkMove = useCallback(async (targetCollectionId, targetFolderId, clearParentFolder = false) => {
    const ids = Array.from(selectedAssetIds);
    if (ids.length === 0) { await showAlert('Chọn ít nhất một item'); return; }
    try {
      await assetsApi.bulkMove(ids, targetCollectionId, targetFolderId, clearParentFolder);
      setSelectedAssetIds(new Set());
      refreshItems();
    } catch (err) {
      console.error('Bulk move error:', err);
      await showAlert('Lỗi khi di chuyển hàng loạt');
    }
  }, [selectedAssetIds, setSelectedAssetIds, refreshItems, alertFn]);

  const handleBulkTag = useCallback(async (tagIds, remove = false) => {
    const ids = Array.from(selectedAssetIds);
    if (ids.length === 0) { await showAlert('Chọn ít nhất một item'); return; }
    try {
      await assetsApi.bulkTag(ids, tagIds, remove);
      refreshItems();
    } catch (err) {
      console.error('Bulk tag error:', err);
      await showAlert('Lỗi khi gán tag hàng loạt');
    }
  }, [selectedAssetIds, refreshItems, alertFn]);

  const handleMoveColorsToGroup = useCallback(
    async (colorIds, targetGroupId, insertBeforeId = null) => {
      if (!colorIds || colorIds.length === 0) return;
      try {
        await assetsApi.bulkMoveGroup(colorIds, targetGroupId, insertBeforeId);
        setSelectedAssetIds(new Set());
        refreshItems();
      } catch (err) {
        console.error('Error moving colors to group:', err);
        await showAlert('Lỗi khi di chuyển màu');
      }
    },
    [setSelectedAssetIds, refreshItems, alertFn],
  );

  return {
    handleBulkDelete,
    handleBulkMove,
    handleBulkTag,
    handleMoveColorsToGroup,
  };
}
