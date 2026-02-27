import client from './client';

/** List all permissions for a collection. */
export async function fetchPermissions(collectionId) {
  const res = await client.get(`/api/collections/${collectionId}/permissions`);
  return res.data;
}

/** Grant permission to a user by email. */
export async function grantPermission(collectionId, { userEmail, role }) {
  const res = await client.post(`/api/collections/${collectionId}/permissions`, { userEmail, role });
  return res.data;
}

/** Update an existing permission role. */
export async function updatePermission(collectionId, permissionId, { role }) {
  const res = await client.put(`/api/collections/${collectionId}/permissions/${permissionId}`, { role });
  return res.data;
}

/** Revoke a permission. */
export async function revokePermission(collectionId, permissionId) {
  await client.delete(`/api/collections/${collectionId}/permissions/${permissionId}`);
}

/** Get current user's role for a collection. */
export async function getMyRole(collectionId) {
  const res = await client.get(`/api/collections/${collectionId}/permissions/my-role`);
  return res.data.role;
}

/** Get all collections shared with the current user. */
export async function fetchSharedCollections() {
  const res = await client.get('/api/shared-collections');
  return res.data;
}
