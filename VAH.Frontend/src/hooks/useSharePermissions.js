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
      setError(err.response?.data?.message || err.response?.data || 'Lỗi khi cấp quyền.');
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
    if (!confirm('Bạn có chắc muốn thu hồi quyền này?')) return;
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
