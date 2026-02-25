import apiClient from './client';

const ENDPOINT = '/Collections';

/** Get all collections */
export const fetchAllCollections = () =>
  apiClient.get(ENDPOINT).then(r => r.data);

/** Get items in a collection (+ sub-collections) */
export const fetchCollectionItems = (collectionId, folderId = null) =>
  apiClient
    .get(`${ENDPOINT}/${collectionId}/items`, { params: { folderId } })
    .then(r => r.data);

/** Create a new collection */
export const createCollection = (payload) =>
  apiClient.post(ENDPOINT, payload).then(r => r.data);

/** Delete a collection */
export const deleteCollection = (id) =>
  apiClient.delete(`${ENDPOINT}/${id}`);
