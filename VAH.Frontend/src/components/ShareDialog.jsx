import { useState, useEffect } from 'react';
import { fetchPermissions, grantPermission, updatePermission, revokePermission } from '../api/permissionsApi';
import './ShareDialog.css';

const ROLES = [
  { value: 'viewer', label: 'Viewer', desc: 'Chỉ xem' },
  { value: 'editor', label: 'Editor', desc: 'Xem & chỉnh sửa' },
  { value: 'owner', label: 'Owner', desc: 'Toàn quyền' }
];

export default function ShareDialog({ collectionId, collectionName, onClose }) {
  const [permissions, setPermissions] = useState([]);
  const [email, setEmail] = useState('');
  const [role, setRole] = useState('viewer');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const loadPermissions = async () => {
    try {
      const data = await fetchPermissions(collectionId);
      setPermissions(data);
    } catch (err) {
      setError('Không thể tải danh sách quyền.');
    }
  };

  useEffect(() => {
    loadPermissions();
  }, [collectionId]);

  const handleGrant = async (e) => {
    e.preventDefault();
    if (!email.trim()) return;
    setLoading(true);
    setError(null);
    try {
      await grantPermission(collectionId, { userEmail: email.trim(), role });
      setEmail('');
      setRole('viewer');
      await loadPermissions();
    } catch (err) {
      setError(err.response?.data?.message || err.response?.data || 'Lỗi khi cấp quyền.');
    } finally {
      setLoading(false);
    }
  };

  const handleUpdateRole = async (permId, newRole) => {
    try {
      await updatePermission(collectionId, permId, { role: newRole });
      await loadPermissions();
    } catch (err) {
      setError('Lỗi khi cập nhật quyền.');
    }
  };

  const handleRevoke = async (permId) => {
    if (!confirm('Bạn có chắc muốn thu hồi quyền này?')) return;
    try {
      await revokePermission(collectionId, permId);
      await loadPermissions();
    } catch (err) {
      setError('Lỗi khi thu hồi quyền.');
    }
  };

  return (
    <div className="share-dialog-overlay" onClick={onClose}>
      <div className="share-dialog" onClick={e => e.stopPropagation()}>
        <div className="share-dialog-header">
          <h3>Chia sẻ: {collectionName}</h3>
          <button className="share-close-btn" onClick={onClose}>×</button>
        </div>

        {error && <div className="share-error">{error}</div>}

        <form className="share-form" onSubmit={handleGrant}>
          <input
            type="email"
            placeholder="Email người dùng..."
            value={email}
            onChange={e => setEmail(e.target.value)}
            disabled={loading}
          />
          <select value={role} onChange={e => setRole(e.target.value)} disabled={loading}>
            {ROLES.map(r => (
              <option key={r.value} value={r.value}>{r.label}</option>
            ))}
          </select>
          <button type="submit" disabled={loading || !email.trim()}>
            {loading ? '...' : 'Chia sẻ'}
          </button>
        </form>

        <div className="share-list">
          {permissions.length === 0 && (
            <p className="share-empty">Chưa chia sẻ cho ai.</p>
          )}
          {permissions.map(p => (
            <div key={p.id} className="share-item">
              <div className="share-user-info">
                <span className="share-user-name">{p.displayName || p.userEmail}</span>
                {p.displayName && <span className="share-user-email">{p.userEmail}</span>}
              </div>
              <select
                value={p.role}
                onChange={e => handleUpdateRole(p.id, e.target.value)}
              >
                {ROLES.map(r => (
                  <option key={r.value} value={r.value}>{r.label}</option>
                ))}
              </select>
              <button className="share-revoke-btn" onClick={() => handleRevoke(p.id)} title="Thu hồi">
                ✕
              </button>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
