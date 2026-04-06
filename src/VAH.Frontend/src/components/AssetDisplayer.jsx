import React from 'react';
import { staticUrl } from '../api/client';
import DraggableAssetCanvas from './DraggableAssetCanvas';
import './AssetDisplayer.css';

const AssetDisplayer = ({
  assets,
  subCollections,
  viewMode,
  onSelectCollection,
  loading
}) => {
  if (loading) {
    return <div className="loading">Đang tải...</div>;
  }

  if (assets.length === 0 && subCollections.length === 0) {
    return (
      <div className="empty-display">
        <p>Không có item nào trong collection này</p>
      </div>
    );
  }

  return (
    <div className="asset-displayer">
      {/* Hiển thị subcollections */}
      {subCollections.length > 0 && (
        <div className="subcollections-section">
          <h3>Thư mục con</h3>
          <div className="subcollections-grid">
            {subCollections.map(collection => (
              <div
                key={collection.id}
                className="subcollection-card"
                onClick={() => onSelectCollection(collection)}
                style={{ borderTopColor: collection.color }}
              >
                <div className="card-icon">
                  {collection.type === 'image' && '🖼️'}
                  {collection.type === 'link' && '🔗'}
                  {collection.type === 'color' && '🎨'}
                  {collection.type === 'default' && '📁'}
                </div>
                <div className="card-name">{collection.name}</div>
                <div className="card-arrow">→</div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Hiển thị assets */}
      {assets.length > 0 && (
        <div className="assets-section">
          <h3>{subCollections.length > 0 ? 'Items' : 'Assets'}</h3>
          {viewMode === 'canvas' ? (
            <DraggableAssetCanvas assets={assets} />
          ) : (
            <div className="assets-gallery">
              {assets.map(asset => (
                <div key={asset.id} className="asset-item">
                  {asset.contentType === 'image' && (
                    <img
                      src={staticUrl(asset.filePath)}
                      alt={asset.fileName}
                      className="asset-image"
                      onError={(e) => {
                        e.target.src = 'data:image/svg+xml,%3Csvg xmlns=%22http://www.w3.org/2000/svg%22 width=%22100%22 height=%22100%22%3E%3Crect fill=%22%23333%22 width=%22100%22 height=%22100%22/%3E%3Ctext fill=%22%23666%22 x=%2250%25%22 y=%2250%25%22 text-anchor=%22middle%22 dy=%22.3em%22%3ENo Image%3C/text%3E%3C/svg%3E';
                      }}
                    />
                  )}
                  {asset.contentType === 'link' && (
                    <div className="asset-link">
                      <div className="link-icon">🔗</div>
                      <div className="link-text">{asset.fileName}</div>
                    </div>
                  )}
                  {asset.contentType === 'color' && (
                    <div className="asset-color" style={{ backgroundColor: asset.filePath }}>
                      <div className="color-code">{asset.filePath}</div>
                    </div>
                  )}
                  <div className="asset-name">{asset.fileName}</div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
};

export default AssetDisplayer;
