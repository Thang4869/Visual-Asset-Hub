import BaseApiService from './BaseApiService';

/**
 * TagApiService — handles tag CRUD and asset-tag junction operations.
 */
class TagApiService extends BaseApiService {
  constructor() {
    super('/Tags');
  }

  /** Get all tags for current user */
  fetchAll() {
    return this._get();
  }

  /** Get a single tag */
  fetch(id) {
    return this._get(`/${id}`);
  }

  /** Create a new tag */
  create(payload) {
    return this._post('', payload);
  }

  /** Update a tag */
  update(id, payload) {
    return this._put(`/${id}`, payload);
  }

  /** Delete a tag */
  delete(id) {
    return this._delete(`/${id}`);
  }

  /** Get tags for a specific asset */
  getAssetTags(assetId) {
    return this._get(`/asset/${assetId}`);
  }

  /** Replace all tags on an asset */
  setAssetTags(assetId, tagIds) {
    return this.client.put(`${this.endpoint}/asset/${assetId}`, { tagIds });
  }

  /** Add tags to an asset */
  addAssetTags(assetId, tagIds) {
    return this._post(`/asset/${assetId}/add`, { tagIds });
  }

  /** Remove tags from an asset */
  removeAssetTags(assetId, tagIds) {
    return this._post(`/asset/${assetId}/remove`, { tagIds });
  }

  /** Migrate legacy comma-separated tags */
  migrate() {
    return this._post('/migrate');
  }
}

const tagApiService = new TagApiService();

// ── Backward-compatible named exports ──
export const fetchAllTags = (...args) => tagApiService.fetchAll(...args);
export const fetchTag = (...args) => tagApiService.fetch(...args);
export const createTag = (...args) => tagApiService.create(...args);
export const updateTag = (...args) => tagApiService.update(...args);
export const deleteTag = (...args) => tagApiService.delete(...args);
export const getAssetTags = (...args) => tagApiService.getAssetTags(...args);
export const setAssetTags = (...args) => tagApiService.setAssetTags(...args);
export const addAssetTags = (...args) => tagApiService.addAssetTags(...args);
export const removeAssetTags = (...args) => tagApiService.removeAssetTags(...args);
export const migrateTags = (...args) => tagApiService.migrate(...args);
export default tagApiService;
