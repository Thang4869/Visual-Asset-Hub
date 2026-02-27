import apiClient from './client';

const ENDPOINT = '/SmartCollections';

/** Get all smart collection definitions */
export const fetchSmartCollections = () =>
  apiClient.get(ENDPOINT).then(r => r.data);

/** Get items in a smart collection */
export const fetchSmartCollectionItems = (id, page = 1, pageSize = 50) =>
  apiClient.get(`${ENDPOINT}/${id}/items`, { params: { page, pageSize } }).then(r => r.data);
