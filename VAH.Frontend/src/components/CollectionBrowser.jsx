import React, { useState, useCallback } from 'react';
import { staticUrl } from '../api/client';
import ContextMenu from './ContextMenu';
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
  selectedFolderIds = new Set(),
  onSelectFolderItem,
  onDeleteFolder,
  onDeleteAsset,
  onRenameAsset,
  onRenameCollection,
  onPinItem,
  onReorder,
  loading,
  searchTerm,
  layoutMode,
  clipboard,
  onCut,
  onCopy,
  onPaste,
  onViewDetail,
  onCreateFolder,
  onCreateLink,
  onUpload,
  pinnedItems = [],
}) => {
  const [contextMenu, setContextMenu] = useState(null);

  const handleContextMenu = useCallback((e, item, type) => {
    e.preventDefault();
    e.stopPropagation();
    setContextMenu({ x: e.clientX, y: e.clientY, item, type });
  }, []);

  const closeContextMenu = useCallback(() => setContextMenu(null), []);

  const getContextMenuItems = (item, type) => {
    const items = [];
    
    items.push({
      label: 'Ghim',
      icon: '📌',
      onClick: () => onPinItem && onPinItem(item, type),
    });

    if (type !== 'collection' && type !== 'folder') {
      items.push({
        label: 'Xem chi tiết',
        icon: '🔍',
        onClick: () => onViewDetail && onViewDetail(item),
      });
    }

    items.push({ divider: true });

    if (type === 'collection') {
      items.push({
        label: 'Sao chép đường dẫn',
        icon: '🔗',
        shortcut: '',
        onClick: () => {
          const path = `/collections/${item.id}`;
          navigator.clipboard?.writeText(window.location.origin + path);
        },
      });
    } else {
      items.push({
        label: 'Sao chép đường dẫn',
        icon: '🔗',
        onClick: () => {
          const path = item.filePath || item.fileName || '';
          navigator.clipboard?.writeText(path);
        },
      });
    }

    items.push({ divider: true });

    items.push({
      label: 'Sao chép',
      icon: '📋',
      shortcut: 'Ctrl+C',
      onClick: () => onCopy && onCopy(item, type),
    });

    items.push({
      label: 'Cắt',
      icon: '✂️',
      shortcut: 'Ctrl+X',
      onClick: () => onCut && onCut(item, type),
    });

    items.push({
      label: 'Dán',
      icon: '📥',
      shortcut: 'Ctrl+V',
      disabled: !clipboard,
      onClick: () => onPaste && onPaste(item, type),
    });

    items.push({ divider: true });

    items.push({
      label: 'Đổi tên',
      icon: '✏️',
      shortcut: 'F2',
      onClick: () => {
        if (type === 'collection') {
          onRenameCollection && onRenameCollection(item);
        } else {
          onRenameAsset && onRenameAsset(item);
        }
      },
    });

    if (type === 'folder') {
      items.push({
        label: 'Xóa thư mục',
        icon: '🗑️',
        onClick: () => onDeleteFolder && onDeleteFolder(item.id),
      });
    } else if (type !== 'collection') {
      items.push({
        label: 'Xóa',
        icon: '🗑️',
        onClick: () => onDeleteAsset && onDeleteAsset(item.id),
      });
    }

    return items;
  };
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

  // Sort pinned items to the top
  const pinnedIds = new Set(pinnedItems.map(p => p.item?.id).filter(Boolean));
  const sortByPinned = (a, b) => {
    const aP = pinnedIds.has(a.id) ? 0 : 1;
    const bP = pinnedIds.has(b.id) ? 0 : 1;
    return aP - bP;
  };
  filteredFolders.sort(sortByPinned);
  filteredFiles.sort(sortByPinned);

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
    <div className="collection-browser"
      onContextMenu={(e) => {
        if (e.target.closest('.browser-item')) return;
        e.preventDefault();
        const areaMenuItems = [];
        if (clipboard) {
          areaMenuItems.push({ label: 'Dán vào đây', icon: '📥', shortcut: 'Ctrl+V', onClick: () => onPaste && onPaste({ id: null }, 'area') });
          areaMenuItems.push({ divider: true });
        }
        if (onCreateFolder) areaMenuItems.push({ label: 'Thư mục mới', icon: '📁', onClick: () => onCreateFolder() });
        if (onCreateLink) areaMenuItems.push({ label: 'Liên kết mới', icon: '🔗', onClick: () => onCreateLink() });
        if (onUpload) areaMenuItems.push({ label: 'Tải lên', icon: '📤', onClick: () => document.querySelector('.upload-area')?.click() });
        if (areaMenuItems.length > 0) setContextMenu({ x: e.clientX, y: e.clientY, item: null, type: 'area', menuItems: areaMenuItems });
      }}
    >
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
                onContextMenu={(e) => handleContextMenu(e, collection, 'collection')}
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
                className={`browser-item folder-item ${selectedFolderIds.has(folder.id) ? 'multi-selected' : ''} ${pinnedIds.has(folder.id) ? 'pinned' : ''}`}
                onClick={(e) => {
                  if (e.ctrlKey || e.metaKey) {
                    // Ctrl+click = multi-select folder
                    onSelectFolderItem && onSelectFolderItem(folder.id, e);
                  } else {
                    // Single click = open folder
                    onSelectFolder && onSelectFolder(folder);
                  }
                }}
                onContextMenu={(e) => handleContextMenu(e, folder, 'folder')}
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
                className={`browser-item file-item ${selectedAssetId === asset.id ? 'selected' : ''} ${selectedAssetIds.has(asset.id) ? 'multi-selected' : ''} ${pinnedIds.has(asset.id) ? 'pinned' : ''}`}
                draggable
                onDragStart={(e) => handleDragStart(e, asset)}
                onClick={(e) => onSelectAsset && onSelectAsset(asset.id, e)}
                onContextMenu={(e) => handleContextMenu(e, asset, asset.contentType)}
              >
                {asset.contentType === 'image' && (
                  <div className="file-preview">
                    <img
                      src={staticUrl(asset.thumbnailMd || asset.thumbnailSm || asset.filePath)}
                      alt={asset.fileName}
                      loading="lazy"
                      onError={(e) => {
                        // If thumbnail fails, try original file
                        if (asset.thumbnailMd && e.target.src !== staticUrl(asset.filePath)) {
                          e.target.src = staticUrl(asset.filePath);
                          return;
                        }
                        e.target.style.display = 'none';
                        e.target.nextElementSibling.style.display = 'flex';
                      }}
                    />
                    <div className="no-preview">📄</div>
                  </div>
                )}
                {asset.contentType === 'link' && (
                  <a
                    className="file-icon link-icon"
                    href={asset.filePath}
                    target="_blank"
                    rel="noopener noreferrer"
                    onClick={(e) => e.stopPropagation()}
                    title={asset.filePath}
                  >
                    🔗
                  </a>
                )}
                {asset.contentType === 'color' && (
                  <div className="file-color" style={{ backgroundColor: asset.filePath }}></div>
                )}
                <div className="item-name">{asset.fileName}</div>
                {asset.contentType === 'link' && asset.filePath && (
                  <a
                    className="item-link-url"
                    href={asset.filePath}
                    target="_blank"
                    rel="noopener noreferrer"
                    onClick={(e) => e.stopPropagation()}
                    title={asset.filePath}
                  >
                    {asset.filePath}
                  </a>
                )}
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

      {/* Context Menu */}
      {contextMenu && (
        <ContextMenu
          x={contextMenu.x}
          y={contextMenu.y}
          items={contextMenu.menuItems || getContextMenuItems(contextMenu.item, contextMenu.type)}
          onClose={closeContextMenu}
        />
      )}
    </div>
  );
};

export default CollectionBrowser;
