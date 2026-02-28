import { useState, useEffect, useCallback } from 'react';
import { fetchPermissions, grantPermission, updatePermission, revokePermission } from '../api/permissionsApi';

/**
 * Hook encapsulating permission CRUD logic for the share dialog.
 *
 * Separates business logic (API calls, state) from presentation (ShareDialog).
 */
export default function useSharePermissions(collectionId) {
  const [permissions, setPermissions] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const loadPermissions = useCallback(async () => {
    if (!collectionId) return;
    try {
      const data = await fetchPermissions(collectionId);
      setPermissions(data);
      setError(null);
    } catch (err) {
      setError('Không thể tải danh sách quyền.');
    }
  }, [collectionId]);

  useEffect(() => {
    loadPermissions();
  }, [loadPermissions]);

  const grant = useCallback(async (email, role) => {
    if (!email.trim()) return;
    setLoading(true);
    setError(null);
    try {
      await grantPermission(collectionId, { userEmail: email.trim(), role });
      await loadPermissions();
    } catch (err) {
      const data = err.response?.data;
      const msg = typeof data === 'string' ? data
        : data?.detail || data?.title || data?.message || 'Lỗi khi cấp quyền.';
      setError(msg);
    } finally {
      setLoading(false);
    }
  }, [collectionId, loadPermissions]);

  const updateRole = useCallback(async (permId, newRole) => {
    try {
      await updatePermission(collectionId, permId, { role: newRole });
      await loadPermissions();
    } catch (err) {
      setError('Lỗi khi cập nhật quyền.');
    }
  }, [collectionId, loadPermissions]);

  const revoke = useCallback(async (permId) => {
    // No confirmation gate here — caller should confirm before invoking
    try {
      await revokePermission(collectionId, permId);
      await loadPermissions();
    } catch (err) {
      setError('Lỗi khi thu hồi quyền.');
    }
  }, [collectionId, loadPermissions]);

  return {
    permissions,
    loading,
    error,
    grant,
    updateRole,
    revoke,
  };
}
