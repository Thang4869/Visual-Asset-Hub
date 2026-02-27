import apiClient from './client';

const ENDPOINT = '/Tags';

/** Get all tags for current user */
export const fetchAllTags = () =>
  apiClient.get(ENDPOINT).then(r => r.data);

/** Get a single tag */
export const fetchTag = (id) =>
  apiClient.get(`${ENDPOINT}/${id}`).then(r => r.data);

/** Create a new tag */
export const createTag = (payload) =>
  apiClient.post(ENDPOINT, payload).then(r => r.data);

/** Update a tag */
export const updateTag = (id, payload) =>
  apiClient.put(`${ENDPOINT}/${id}`, payload).then(r => r.data);

/** Delete a tag */
export const deleteTag = (id) =>
  apiClient.delete(`${ENDPOINT}/${id}`);

/** Get tags for a specific asset */
export const getAssetTags = (assetId) =>
  apiClient.get(`${ENDPOINT}/asset/${assetId}`).then(r => r.data);

/** Replace all tags on an asset */
export const setAssetTags = (assetId, tagIds) =>
  apiClient.put(`${ENDPOINT}/asset/${assetId}`, { tagIds });

/** Add tags to an asset */
export const addAssetTags = (assetId, tagIds) =>
  apiClient.post(`${ENDPOINT}/asset/${assetId}/add`, { tagIds });

/** Remove tags from an asset */
export const removeAssetTags = (assetId, tagIds) =>
  apiClient.post(`${ENDPOINT}/asset/${assetId}/remove`, { tagIds });

/** Migrate legacy comma-separated tags */
export const migrateTags = () =>
  apiClient.post(`${ENDPOINT}/migrate`).then(r => r.data);
