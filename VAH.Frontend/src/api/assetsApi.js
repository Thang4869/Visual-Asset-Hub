import apiClient from './client';

const ENDPOINT = '/Assets';

/** Upload files to a collection */
export const uploadFiles = (collectionId, files, folderId = null) => {
  const formData = new FormData();
  files.forEach((file) => formData.append('files', file));

  const params = new URLSearchParams();
  params.set('collectionId', collectionId);
  if (folderId) params.set('folderId', folderId);

  return apiClient.post(`${ENDPOINT}/upload?${params.toString()}`, formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
};

/** Create a folder */
export const createFolder = (payload) =>
  apiClient.post(`${ENDPOINT}/create-folder`, payload).then(r => r.data);

/** Create a link */
export const createLink = (payload) =>
  apiClient.post(`${ENDPOINT}/create-link`, payload).then(r => r.data);

/** Create a color */
export const createColor = (payload) =>
  apiClient.post(`${ENDPOINT}/create-color`, payload).then(r => r.data);

/** Create a color group */
export const createColorGroup = (payload) =>
  apiClient.post(`${ENDPOINT}/create-color-group`, payload).then(r => r.data);

/** Update asset (rename, move, etc.) */
export const updateAsset = (id, payload) =>
  apiClient.put(`${ENDPOINT}/${id}`, payload).then(r => r.data);

/** Update asset position (canvas) */
export const updatePosition = (id, positionX, positionY) =>
  apiClient.put(`${ENDPOINT}/${id}/position`, { positionX, positionY });

/** Delete an asset */
export const deleteAsset = (id) =>
  apiClient.delete(`${ENDPOINT}/${id}`);

/** Reorder assets */
export const reorderAssets = (assetIds) =>
  apiClient.post(`${ENDPOINT}/reorder`, { assetIds });

// ──── Bulk Operations ────

/** Bulk delete assets */
export const bulkDelete = (assetIds) =>
  apiClient.post(`${ENDPOINT}/bulk-delete`, { assetIds }).then(r => r.data);

/** Bulk move assets */
export const bulkMove = (assetIds, targetCollectionId = null, targetFolderId = null, clearParentFolder = false) =>
  apiClient.post(`${ENDPOINT}/bulk-move`, { assetIds, targetCollectionId, targetFolderId, clearParentFolder }).then(r => r.data);

/** Bulk tag assets */
export const bulkTag = (assetIds, tagIds, remove = false) =>
  apiClient.post(`${ENDPOINT}/bulk-tag`, { assetIds, tagIds, remove }).then(r => r.data);
