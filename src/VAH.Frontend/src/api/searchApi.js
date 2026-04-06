import BaseApiService from './BaseApiService';

/**
 * SearchApiService — handles server-side search.
 */
class SearchApiService extends BaseApiService {
  constructor() {
    super('/Search');
  }

  /**
   * Server-side search across assets and collections.
   * @param {Object} params - Search parameters
   * @param {string} [params.q] - Search query
   * @param {string} [params.type] - Content type filter (image, link, color, etc.)
   * @param {number} [params.collectionId] - Limit search to a specific collection
   * @param {number} [params.page=1] - Page number
   * @param {number} [params.pageSize=50] - Items per page
   * @returns {Promise<{query, assets, totalAssets, collections, totalCollections, page, pageSize, hasNextPage}>}
   */
  search(params = {}) {
    return this._get('', params);
  }
}

const searchApiService = new SearchApiService();

// ── Backward-compatible named export ──
export const search = (...args) => searchApiService.search(...args);
export default searchApiService;
