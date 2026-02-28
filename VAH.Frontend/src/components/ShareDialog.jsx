import { useState } from 'react';
import useSharePermissions from '../hooks/useSharePermissions';
import './ShareDialog.css';

const ROLES = [
  { value: 'viewer', label: 'Viewer', desc: 'Chỉ xem' },
  { value: 'editor', label: 'Editor', desc: 'Xem & chỉnh sửa' },
  { value: 'owner', label: 'Owner', desc: 'Toàn quyền' }
];

export default function ShareDialog({ collectionId, collectionName, onClose }) {
  const [email, setEmail] = useState('');
  const [role, setRole] = useState('viewer');
  const { permissions, loading, error, grant, updateRole, revoke } = useSharePermissions(collectionId);

  const handleGrant = async (e) => {
    e.preventDefault();
    await grant(email, role);
    setEmail('');
    setRole('viewer');
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
                onChange={e => updateRole(p.id, e.target.value)}
              >
                {ROLES.map(r => (
                  <option key={r.value} value={r.value}>{r.label}</option>
                ))}
              </select>
              <button className="share-revoke-btn" onClick={() => revoke(p.id)} title="Thu hồi">
                ✕
              </button>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
