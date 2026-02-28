import BaseApiService from './BaseApiService';

/**
 * AssetApiService — handles asset CRUD and bulk operations.
 */
class AssetApiService extends BaseApiService {
  constructor() {
    super('/Assets');
  }

  /** Upload files to a collection */
  uploadFiles(collectionId, files, folderId = null) {
    const formData = new FormData();
    files.forEach((file) => formData.append('files', file));

    const params = new URLSearchParams();
    params.set('collectionId', collectionId);
    if (folderId) params.set('folderId', folderId);

    return this.client.post(`${this.endpoint}/upload?${params.toString()}`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  }

  /** Create a folder */
  createFolder(payload) {
    return this._post('/folders', payload);
  }

  /** Create a link */
  createLink(payload) {
    return this._post('/links', payload);
  }

  /** Create a color */
  createColor(payload) {
    return this._post('/colors', payload);
  }

  /** Create a color group */
  createColorGroup(payload) {
    return this._post('/color-groups', payload);
  }

  /** Update asset (rename, move, etc.) — PATCH semantics */
  updateAsset(id, payload) {
    return this._patch(`/${id}`, payload);
  }

  /** Update asset position (canvas) */
  updatePosition(id, positionX, positionY) {
    return this.client.put(`${this.endpoint}/${id}/position`, { positionX, positionY });
  }

  /** Delete an asset */
  deleteAsset(id) {
    return this.client.delete(`${this.endpoint}/${id}`);
  }

  /** Reorder assets */
  reorderAssets(assetIds) {
    return this.client.post(`${this.endpoint}/reorder`, { assetIds });
  }

  // ──── Bulk Operations ────

  /** Bulk delete assets */
  bulkDelete(assetIds) {
    return this._post('/bulk-delete', { assetIds });
  }

  /** Bulk move assets */
  bulkMove(assetIds, targetCollectionId = null, targetFolderId = null, clearParentFolder = false) {
    return this._post('/bulk-move', { assetIds, targetCollectionId, targetFolderId, clearParentFolder });
  }

  /** Bulk move colors to a group with positional insert */
  bulkMoveGroup(assetIds, targetGroupId = null, insertBeforeId = null) {
    return this._post('/bulk-move-group', { assetIds, targetGroupId, insertBeforeId });
  }

  /** Bulk tag assets */
  bulkTag(assetIds, tagIds, remove = false) {
    return this._post('/bulk-tag', { assetIds, tagIds, remove });
  }

  /** Duplicate an asset */
  duplicateAsset(id, targetFolderId = null) {
    return this.client.post(`${this.endpoint}/${id}/duplicate`, { targetFolderId });
  }
}

const assetApiService = new AssetApiService();

// ── Backward-compatible named exports ──
export const uploadFiles = (...args) => assetApiService.uploadFiles(...args);
export const createFolder = (...args) => assetApiService.createFolder(...args);
export const createLink = (...args) => assetApiService.createLink(...args);
export const createColor = (...args) => assetApiService.createColor(...args);
export const createColorGroup = (...args) => assetApiService.createColorGroup(...args);
export const updateAsset = (...args) => assetApiService.updateAsset(...args);
export const updatePosition = (...args) => assetApiService.updatePosition(...args);
export const deleteAsset = (...args) => assetApiService.deleteAsset(...args);
export const reorderAssets = (...args) => assetApiService.reorderAssets(...args);
export const bulkDelete = (...args) => assetApiService.bulkDelete(...args);
export const bulkMove = (...args) => assetApiService.bulkMove(...args);
export const bulkMoveGroup = (...args) => assetApiService.bulkMoveGroup(...args);
export const bulkTag = (...args) => assetApiService.bulkTag(...args);
export const duplicateAsset = (...args) => assetApiService.duplicateAsset(...args);
export default assetApiService;
