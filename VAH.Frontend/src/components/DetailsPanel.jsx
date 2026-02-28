import React from 'react';
import { staticUrl } from '../api/client';

/**
 * Right-side details panel showing asset info, tags, and preview.
 */
export default function DetailsPanel({
  asset,
  collectionName,
  onClose,
  onAddTag,
}) {
  if (!asset) return null;

  const formatDate = (dateStr) => {
    if (!dateStr) return '—';
    const d = new Date(dateStr);
    return d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
  };

  const getContentTypeLabel = (type) => {
    const map = { image: 'Hình ảnh', link: 'Liên kết', color: 'Màu sắc', folder: 'Thư mục', file: 'Tệp tin', default: 'Tệp tin' };
    return map[type] || type;
  };

  const tagList = asset.tags ? asset.tags.split(',').filter(Boolean) : [];

  return (
    <aside className="details-panel">
      <div className="details-panel-header">
        <h3>Tài liệu</h3>
        <button className="details-close-btn" onClick={onClose} title="Đóng">
          <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
        </button>
      </div>

      {/* Large icon / thumbnail */}
      <div className="details-preview">
        {asset.contentType === 'image' ? (
          <img
            src={staticUrl(asset.filePath)}
            alt={asset.fileName}
            onError={(e) => { e.target.style.display = 'none'; }}
          />
        ) : asset.contentType === 'color' ? (
          <div style={{ width: 120, height: 120, borderRadius: 12, backgroundColor: asset.filePath, boxShadow: '0 4px 16px rgba(0,0,0,0.3)' }} />
        ) : (
          <div className="details-preview-icon">
            {asset.isFolder ? '📁' : asset.contentType === 'link' ? '🔗' : '📄'}
          </div>
        )}
      </div>

      {/* Info section */}
      <div className="details-info">
        <h3 className="details-info-title">{asset.fileName}</h3>

        <div className="details-info-section">
          <h4>Thông tin</h4>
          <div className="details-info-row">
            <span className="details-info-label">Title</span>
            <span className="details-info-value">{asset.fileName}</span>
          </div>
          <div className="details-info-row">
            <span className="details-info-label">Đơn giữ/đặn</span>
            <span className="details-info-value">{collectionName || '—'}</span>
          </div>
          <div className="details-info-row">
            <span className="details-info-label">Tải lên</span>
            <span className="details-info-value">{formatDate(asset.createdAt)}</span>
          </div>
          <div className="details-info-row">
            <span className="details-info-label">Loại tệp</span>
            <span className="details-info-value">{getContentTypeLabel(asset.contentType)}</span>
          </div>
          <div className="details-info-row">
            <span className="details-info-label">Thư mục gốc</span>
            <span className="details-info-value" style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
              {asset.isFolder ? '—' : '■'}
            </span>
          </div>

          {/* Tags section */}
          <div className="details-tags-section">
            <h4>Tags</h4>
            <div className="details-tags-list">
              {tagList.map((tag, i) => (
                <span key={i} className="tag-badge">{tag.trim()}</span>
              ))}
              {tagList.length === 0 && (
                <span className="details-info-value" style={{ opacity: 0.5 }}>Chưa có tag</span>
              )}
            </div>
            <button className="btn-small" onClick={() => onAddTag(asset.id)}>+ Tag</button>
          </div>
        </div>
      </div>

      {/* Preview section for images */}
      {asset.contentType === 'image' && (
        <div className="details-preview-section">
          <h4>Chỉ xem</h4>
          <div className="details-full-preview">
            <img
              src={staticUrl(asset.filePath)}
              alt={asset.fileName}
              onError={(e) => { e.target.parentElement.style.display = 'none'; }}
            />
          </div>
        </div>
      )}
    </aside>
  );
}
