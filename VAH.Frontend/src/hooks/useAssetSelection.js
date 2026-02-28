import { useState, useCallback } from 'react';

/**
 * Hook encapsulating asset selection state (single + multi-select).
 *
 * Supports:
 *  - Normal click → single select
 *  - Ctrl/Cmd + click → toggle individual
 *  - Shift + click → range select
 *  - Select all / clear
 */
export default function useAssetSelection(collectionItems) {
  const [selectedAssetId, setSelectedAssetId] = useState(null);
  const [selectedAssetIds, setSelectedAssetIds] = useState(new Set());

  const selectedAsset = selectedAssetId
    ? collectionItems.items.find((a) => a.id === selectedAssetId)
    : null;

  const toggleSelectAsset = useCallback((assetId, event) => {
    if (event?.ctrlKey || event?.metaKey) {
      setSelectedAssetIds((prev) => {
        const next = new Set(prev);
        if (next.has(assetId)) next.delete(assetId);
        else next.add(assetId);
        return next;
      });
    } else if (event?.shiftKey && selectedAssetId) {
      const items = collectionItems.items;
      const startIdx = items.findIndex((a) => a.id === selectedAssetId);
      const endIdx = items.findIndex((a) => a.id === assetId);
      if (startIdx !== -1 && endIdx !== -1) {
        const [lo, hi] = startIdx < endIdx ? [startIdx, endIdx] : [endIdx, startIdx];
        const range = items.slice(lo, hi + 1).map((a) => a.id);
        setSelectedAssetIds((prev) => {
          const next = new Set(prev);
          range.forEach((id) => next.add(id));
          return next;
        });
      }
    } else {
      setSelectedAssetId(assetId);
      setSelectedAssetIds(new Set());
    }
  }, [selectedAssetId, collectionItems]);

  const selectAllAssets = useCallback(() => {
    const ids = collectionItems.items.filter((a) => !a.isFolder).map((a) => a.id);
    setSelectedAssetIds(new Set(ids));
  }, [collectionItems]);

  const clearSelection = useCallback(() => {
    setSelectedAssetIds(new Set());
  }, []);

  return {
    selectedAssetId,
    setSelectedAssetId,
    selectedAsset,
    selectedAssetIds,
    setSelectedAssetIds,
    toggleSelectAsset,
    selectAllAssets,
    clearSelection,
  };
}
