import React from 'react';
import { staticUrl } from '../api/client';
import './CollectionBrowser.css';

const CollectionBrowser = ({
  assets,
  subCollections,
  onSelectCollection,
  onSelectFolder,
  onMoveAsset,
  onSelectAsset,
  selectedAssetId,
  selectedAssetIds = new Set(),
  onReorder,
  loading,
  searchTerm,
  layoutMode
}) => {
  if (loading) {
    return <div className="loading">Đang tải...</div>;
  }

  // Filter by search term
  const filteredSubCollections = subCollections.filter(c =>
    c.name.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const folderAssets = assets.filter(a => a.isFolder);
  const fileAssets = assets.filter(a => !a.isFolder && a.contentType !== 'color-group');

  const filteredFolders = folderAssets.filter(a =>
    a.fileName.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const filteredFiles = fileAssets.filter(a =>
    a.fileName.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const isEmpty = filteredSubCollections.length === 0 && filteredFolders.length === 0 && filteredFiles.length === 0;
  const canReorder = layoutMode === 'list' && !searchTerm;

  const handleDragStart = (e, asset) => {
    e.dataTransfer.setData('text/plain', asset.id.toString());
  };

  const handleDropOnFolder = (e, folder) => {
    e.preventDefault();
    const assetId = e.dataTransfer.getData('text/plain');
    if (assetId && onMoveAsset) {
      onMoveAsset(parseInt(assetId, 10), folder.id);
    }
  };

  const handleReorder = (fromIndex, toIndex) => {
    if (!canReorder || !onReorder) return;
    if (toIndex < 0 || toIndex >= filteredFiles.length) return;
    const updated = [...filteredFiles];
    const [moved] = updated.splice(fromIndex, 1);
    updated.splice(toIndex, 0, moved);
    onReorder(updated.map(a => a.id));
  };

  const formatDate = (dateStr) => {
    if (!dateStr) return '';
    const d = new Date(dateStr);
    return d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });
  };

  return (
    <div className="collection-browser">
      {/* Folders Section */}
      {(filteredSubCollections.length > 0 || filteredFolders.length > 0) && (
        <section className="browser-section">
          <h3 className="section-title">Thư mục</h3>
          <div className="browser-grid folders-grid">
            {filteredSubCollections.map(collection => (
              <div
                key={`collection-${collection.id}`}
                className="browser-item folder-item"
                onClick={() => onSelectCollection(collection)}
              >
                <div className="item-icon">🗂️</div>
                <div className="item-name">{collection.name}</div>
                <div className="item-meta">
                  <span className="item-type-badge">{collection.type}</span>
                </div>
              </div>
            ))}
            {filteredFolders.map(folder => (
              <div
                key={`folder-${folder.id}`}
                className="browser-item folder-item"
                onClick={() => onSelectFolder && onSelectFolder(folder)}
                onDragOver={(e) => e.preventDefault()}
                onDrop={(e) => handleDropOnFolder(e, folder)}
              >
                <div className="item-icon">📁</div>
                <div className="item-name">{folder.fileName}</div>
                <div className="item-meta">
                  <span className="item-date">{formatDate(folder.createdAt)}</span>
                </div>
              </div>
            ))}
          </div>
        </section>
      )}

      {/* Files Section */}
      {filteredFiles.length > 0 && (
        <section className="browser-section">
          <h3 className="section-title">Tệp tin</h3>
          <div className={`browser-grid files-grid layout-${layoutMode}`}>
            {filteredFiles.map((asset, index) => (
              <div
                key={asset.id}
                className={`browser-item file-item ${selectedAssetId === asset.id ? 'selected' : ''} ${selectedAssetIds.has(asset.id) ? 'multi-selected' : ''}`}
                draggable
                onDragStart={(e) => handleDragStart(e, asset)}
                onClick={(e) => onSelectAsset && onSelectAsset(asset.id, e)}
              >
                {asset.contentType === 'image' && (
                  <div className="file-preview">
                    <img
                      src={staticUrl(asset.filePath)}
                      alt={asset.fileName}
                      onError={(e) => {
                        e.target.style.display = 'none';
                        e.target.nextElementSibling.style.display = 'flex';
                      }}
                    />
                    <div className="no-preview">📄</div>
                  </div>
                )}
                {asset.contentType === 'link' && (
                  <div className="file-icon link-icon">🔗</div>
                )}
                {asset.contentType === 'color' && (
                  <div className="file-color" style={{ backgroundColor: asset.filePath }}></div>
                )}
                <div className="item-name">{asset.fileName}</div>
                <div className="item-meta">
                  <span className="item-date">{formatDate(asset.createdAt)}</span>
                </div>
                {canReorder && (
                  <div className="reorder-controls">
                    <button onClick={(e) => { e.stopPropagation(); handleReorder(index, index - 1); }}>▲</button>
                    <button onClick={(e) => { e.stopPropagation(); handleReorder(index, index + 1); }}>▼</button>
                  </div>
                )}
              </div>
            ))}
          </div>
        </section>
      )}

      {isEmpty && (
        <div className="empty-browser">
          <div className="empty-icon">📭</div>
          <p>Thư mục này trống</p>
          {searchTerm && <p className="search-hint">Không tìm thấy "{searchTerm}"</p>}
        </div>
      )}
    </div>
  );
};

export default CollectionBrowser;
