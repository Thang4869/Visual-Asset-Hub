/**
 * API Service barrel — re-exports all API service singletons.
 *
 * Usage:
 *   import { assetApi, collectionApi } from '../api';
 *   const items = await collectionApi.fetchItems(id);
 */
export { default as tokenManager } from './TokenManager';
export { default as assetApi } from './assetsApi';
export { default as authApi } from './authApi';
export { default as collectionApi } from './collectionsApi';
export { default as tagApi } from './tagsApi';
export { default as searchApi } from './searchApi';
export { default as smartCollectionApi } from './smartCollectionsApi';
export { default as permissionApi } from './permissionsApi';
export { staticUrl, STATIC_URL } from './client';
