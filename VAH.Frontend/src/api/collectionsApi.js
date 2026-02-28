import BaseApiService from './BaseApiService';

/**
 * CollectionApiService — handles collection CRUD operations.
 */
class CollectionApiService extends BaseApiService {
  constructor() {
    super('/Collections');
  }

  /** Get all collections */
  fetchAll() {
    return this._get();
  }

  /** Get items in a collection (+ sub-collections) */
  fetchItems(collectionId, folderId = null) {
    return this._get(`/${collectionId}/items`, { folderId });
  }

  /** Create a new collection */
  create(payload) {
    return this._post('', payload);
  }

  /** Update a collection (partial) — PATCH semantics */
  update(id, payload) {
    return this._patch(`/${id}`, { id, ...payload });
  }

  /** Delete a collection */
  delete(id) {
    return this._delete(`/${id}`);
  }
}

const collectionApiService = new CollectionApiService();

// ── Backward-compatible named exports ──
export const fetchAllCollections = (...args) => collectionApiService.fetchAll(...args);
export const fetchCollectionItems = (...args) => collectionApiService.fetchItems(...args);
export const createCollection = (...args) => collectionApiService.create(...args);
export const updateCollection = (...args) => collectionApiService.update(...args);
export const deleteCollection = (...args) => collectionApiService.delete(...args);
export default collectionApiService;
