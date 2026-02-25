import apiClient from './client';

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
export const search = (params = {}) =>
  apiClient.get('/Search', { params }).then(r => r.data);
