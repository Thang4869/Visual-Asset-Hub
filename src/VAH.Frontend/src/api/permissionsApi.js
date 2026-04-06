import BaseApiService from './BaseApiService';

/**
 * PermissionApiService — handles collection permission endpoints.
 *
 * Note: These endpoints use slightly different URL patterns
 * (/api/collections/:id/permissions) which are relative to the apiClient baseURL.
 */
class PermissionApiService extends BaseApiService {
  constructor() {
    // No fixed endpoint — paths are built per-collection
    super('');
  }

  /** @private Build the permissions base path for a collection */
  _permPath(collectionId) {
    return `/collections/${collectionId}/permissions`;
  }

  /** List all permissions for a collection. */
  fetchPermissions(collectionId) {
    return this._get(this._permPath(collectionId));
  }

  /** Grant permission to a user by email. */
  grantPermission(collectionId, { userEmail, role }) {
    return this._post(this._permPath(collectionId), { userEmail, role });
  }

  /** Update an existing permission role. */
  updatePermission(collectionId, permissionId, { role }) {
    return this._put(`${this._permPath(collectionId)}/${permissionId}`, { role });
  }

  /** Revoke a permission. */
  async revokePermission(collectionId, permissionId) {
    await this.client.delete(`${this._permPath(collectionId)}/${permissionId}`);
  }

  /** Get current user's role for a collection. */
  async getMyRole(collectionId) {
    const res = await this.client.get(`${this._permPath(collectionId)}/my-role`);
    return res.data.role;
  }

  /** Get all collections shared with the current user. */
  fetchSharedCollections() {
    return this._get('/shared-collections');
  }
}

const permissionApiService = new PermissionApiService();

// ── Backward-compatible named exports ──
export const fetchPermissions = (...args) => permissionApiService.fetchPermissions(...args);
export const grantPermission = (...args) => permissionApiService.grantPermission(...args);
export const updatePermission = (...args) => permissionApiService.updatePermission(...args);
export const revokePermission = (...args) => permissionApiService.revokePermission(...args);
export const getMyRole = (...args) => permissionApiService.getMyRole(...args);
export const fetchSharedCollections = (...args) => permissionApiService.fetchSharedCollections(...args);
export default permissionApiService;
