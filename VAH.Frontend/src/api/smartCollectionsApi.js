import BaseApiService from './BaseApiService';

/**
 * SmartCollectionApiService — handles smart (auto-generated) collection endpoints.
 */
class SmartCollectionApiService extends BaseApiService {
  constructor() {
    super('/SmartCollections');
  }

  /** Get all smart collection definitions */
  fetchAll() {
    return this._get();
  }

  /** Get items in a smart collection */
  fetchItems(id, page = 1, pageSize = 50) {
    return this._get(`/${id}/items`, { page, pageSize });
  }
}

const smartCollectionApiService = new SmartCollectionApiService();

// ── Backward-compatible named exports ──
export const fetchSmartCollections = (...args) => smartCollectionApiService.fetchAll(...args);
export const fetchSmartCollectionItems = (...args) => smartCollectionApiService.fetchItems(...args);
export default smartCollectionApiService;
