import React, { useState, useCallback, useEffect } from 'react';
import { fetchCollectionItems } from '../api/collectionsApi';
import ContextMenu from './ContextMenu';
import './TreeViewPanel.css';

/**
 * Tree View Panel — right sidebar showing hierarchical structure of current collection.
 * 
 * Shows: subCollections (children) + folder assets + file assets in a tree.
 * Each node is collapsible.
 */
export default function TreeViewPanel({
  collection,
  subCollections = [],
  items = [],
  selectedAssetId,
  onSelectAsset,
  onSelectFolder,
  onSelectCollection,
  onContextMenu,
  collapsed,
  onToggleCollapsed,
  clipboard,
  onCopy,
  onCut,
  onPaste,
  onPinItem,
  onRenameAsset,
  onRenameCollection,
  onDeleteFolder,
  onDeleteAsset,
}) {
  const [expandedIds, setExpandedIds] = useState(new Set(['root']));
  const [contextMenu, setContextMenu] = useState(null);

  const toggleExpand = useCallback((id) => {
    setExpandedIds(prev => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }, []);

  const collapseAll = useCallback(() => {
    setExpandedIds(new Set());
  }, []);

  const expandAll = useCallback(() => {
    const ids = new Set(['root']);
    subCollections.forEach(c => ids.add(`col-${c.id}`));
    items.filter(a => a.isFolder).forEach(f => ids.add(`folder-${f.id}`));
    items.filter(a => a.contentType === 'color-group').forEach(g => ids.add(`group-${g.id}`));
    setExpandedIds(ids);
  }, [subCollections, items]);

  if (!collection) return null;

  const folders = items.filter(a => a.isFolder);
  const files = items.filter(a => !a.isFolder);

  // For color collections, separate groups and their children
  const isColorCollection = collection.type === 'color';
  const colorGroups = isColorCollection ? items.filter(a => a.contentType === 'color-group') : [];
  const colorItems = isColorCollection ? items.filter(a => a.contentType === 'color') : [];
  const nonColorFiles = isColorCollection
    ? files.filter(a => a.contentType !== 'color' && a.contentType !== 'color-group')
    : files;

  const hasChildren = subCollections.length > 0 || folders.length > 0 || files.length > 0;
  const isRootExpanded = expandedIds.has('root');

  const getTypeIcon = (type) => {
    switch(type) {
      case 'image': return '🖼️';
      case 'link': return '🔗';
      case 'color': return '🎨';
      default: return '📁';
    }
  };

  const getAssetIcon = (asset) => {
    if (asset.isFolder) return '📁';
    if (asset.contentType === 'image') return '🖼️';
    if (asset.contentType === 'link') return '🔗';
    if (asset.contentType === 'color') return '🎨';
    if (asset.contentType === 'colorGroup') return '🎯';
    return '📄';
  };

  const handleItemContextMenu = (e, item, type) => {
    e.preventDefault();
    e.stopPropagation();
    setContextMenu({ x: e.clientX, y: e.clientY, item, type });
  };

  const closeContextMenu = useCallback(() => setContextMenu(null), []);

  const getContextMenuItems = (item, type) => {
    const menuItems = [];
    
    menuItems.push({
      label: 'Ghim',
      icon: '📌',
      onClick: () => onPinItem && onPinItem(item, type),
    });

    menuItems.push({ divider: true });

    menuItems.push({
      label: 'Sao chép đường dẫn',
      icon: '🔗',
      onClick: () => {
        const path = type === 'collection' 
          ? `${window.location.origin}/collections/${item.id}`
          : (item.filePath || item.fileName || '');
        navigator.clipboard?.writeText(path);
      },
    });

    menuItems.push({ divider: true });

    menuItems.push({
      label: 'Sao chép',
      icon: '📋',
      shortcut: 'Ctrl+C',
      onClick: () => onCopy && onCopy(item, type),
    });

    menuItems.push({
      label: 'Cắt',
      icon: '✂️',
      shortcut: 'Ctrl+X',
      onClick: () => onCut && onCut(item, type),
    });

    menuItems.push({
      label: 'Dán',
      icon: '📥',
      shortcut: 'Ctrl+V',
      disabled: !clipboard,
      onClick: () => onPaste && onPaste(item, type),
    });

    menuItems.push({ divider: true });

    menuItems.push({
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
      menuItems.push({
        label: 'Xóa thư mục',
        icon: '🗑️',
        onClick: () => onDeleteFolder && onDeleteFolder(item.id),
      });
    } else if (type !== 'collection') {
      menuItems.push({
        label: 'Xóa',
        icon: '🗑️',
        onClick: () => onDeleteAsset && onDeleteAsset(item.id),
      });
    }

    return menuItems;
  };

  return (
    <aside className={`tree-view-panel ${collapsed ? 'collapsed' : ''}`}>
      <div className="tree-view-header">
        {!collapsed && (
          <>
            <h3>Cấu trúc</h3>
            <div className="tree-view-actions">
              <button
                className="tree-action-btn"
                onClick={expandAll}
                title="Mở rộng tất cả"
              >
                <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><path d="M7 13l5 5 5-5"/><path d="M7 6l5 5 5-5"/></svg>
              </button>
              <button
                className="tree-action-btn"
                onClick={collapseAll}
                title="Thu gọn tất cả"
              >
                <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><path d="M7 11l5-5 5 5"/><path d="M7 18l5-5 5 5"/></svg>
              </button>
            </div>
          </>
        )}
        <button
          className="tree-toggle-btn"
          onClick={onToggleCollapsed}
          title={collapsed ? 'Mở Tree View' : 'Thu gọn Tree View'}
        >
          <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
            {collapsed ? (
              <><line x1="3" y1="12" x2="21" y2="12"/><line x1="3" y1="6" x2="21" y2="6"/><line x1="3" y1="18" x2="21" y2="18"/><polyline points="15 6 9 12 15 18"/></>
            ) : (
              <><line x1="3" y1="12" x2="21" y2="12"/><line x1="3" y1="6" x2="21" y2="6"/><line x1="3" y1="18" x2="21" y2="18"/><polyline points="9 6 15 12 9 18"/></>
            )}
          </svg>
        </button>
      </div>

      {!collapsed && (
        <div className="tree-view-content">
          {/* Root node = current collection */}
          <div className="tree-node">
            <div
              className="tree-node-row root-node"
              onContextMenu={(e) => handleItemContextMenu(e, collection, 'collection')}
            >
              <button
                className="tree-expand-btn"
                onClick={() => toggleExpand('root')}
              >
                {hasChildren ? (isRootExpanded ? '▾' : '▸') : ''}
              </button>
              <span className="tree-node-icon">{getTypeIcon(collection.type)}</span>
              <span className="tree-node-name">{collection.name}</span>
              <span className="tree-node-count">
                {subCollections.length + folders.length + files.length}
              </span>
            </div>

            {isRootExpanded && hasChildren && (
              <div className="tree-children">
                {/* Sub-collections */}
                {subCollections.map(sub => (
                  <TreeSubCollection
                    key={`col-${sub.id}`}
                    collection={sub}
                    expandedIds={expandedIds}
                    onToggle={toggleExpand}
                    onClick={() => onSelectCollection && onSelectCollection(sub)}
                    onContextMenu={(e) => handleItemContextMenu(e, sub, 'collection')}
                  />
                ))}

                {/* Folders */}
                {folders.map(folder => (
                  <TreeFolder
                    key={`folder-${folder.id}`}
                    folder={folder}
                    collectionId={collection.id}
                    expandedIds={expandedIds}
                    onToggle={toggleExpand}
                    selectedAssetId={selectedAssetId}
                    onClick={() => onSelectFolder && onSelectFolder(folder)}
                    onSelectAsset={onSelectAsset}
                    onSelectFolder={onSelectFolder}
                    onContextMenu={(e) => handleItemContextMenu(e, folder, 'folder')}
                    onItemContextMenu={handleItemContextMenu}
                    getAssetIcon={getAssetIcon}
                  />
                ))}

                {/* Color groups with nested colors */}
                {isColorCollection && colorGroups.map(group => {
                  const groupId = `group-${group.id}`;
                  const isGroupExpanded = expandedIds.has(groupId);
                  const childColors = colorItems.filter(c => c.groupId === group.id);
                  return (
                    <div key={groupId} className="tree-node">
                      <div
                        className={`tree-node-row ${selectedAssetId === group.id ? 'selected' : ''}`}
                        onClick={() => onSelectAsset && onSelectAsset(group.id)}
                        onContextMenu={(e) => handleItemContextMenu(e, group, 'color-group')}
                      >
                        <button
                          className="tree-expand-btn"
                          onClick={(e) => { e.stopPropagation(); toggleExpand(groupId); }}
                        >
                          {childColors.length > 0 ? (isGroupExpanded ? '▾' : '▸') : ''}
                        </button>
                        <span className="tree-node-icon">🎯</span>
                        <span className="tree-node-name" title={group.fileName}>{group.fileName}</span>
                        <span className="tree-node-count">{childColors.length}</span>
                      </div>
                      {isGroupExpanded && childColors.length > 0 && (
                        <div className="tree-children">
                          {childColors.map(color => (
                            <div
                              key={`color-${color.id}`}
                              className={`tree-node-row tree-leaf ${selectedAssetId === color.id ? 'selected' : ''}`}
                              onClick={() => onSelectAsset && onSelectAsset(color.id)}
                              onContextMenu={(e) => handleItemContextMenu(e, color, 'color')}
                            >
                              <span className="tree-expand-btn" />
                              <span
                                className="tree-color-swatch"
                                style={{ backgroundColor: color.filePath }}
                              />
                              <span className="tree-node-name" title={color.filePath}>{color.filePath}</span>
                            </div>
                          ))}
                        </div>
                      )}
                    </div>
                  );
                })}

                {/* Ungrouped colors */}
                {isColorCollection && (() => {
                  const ungrouped = colorItems.filter(c => !c.groupId);
                  if (ungrouped.length === 0) return null;
                  return ungrouped.map(color => (
                    <div
                      key={`color-${color.id}`}
                      className={`tree-node-row tree-leaf ${selectedAssetId === color.id ? 'selected' : ''}`}
                      onClick={() => onSelectAsset && onSelectAsset(color.id)}
                      onContextMenu={(e) => handleItemContextMenu(e, color, 'color')}
                    >
                      <span className="tree-expand-btn" />
                      <span
                        className="tree-color-swatch"
                        style={{ backgroundColor: color.filePath }}
                      />
                      <span className="tree-node-name" title={color.filePath}>{color.filePath}</span>
                    </div>
                  ));
                })()}

                {/* Non-color files (for non-color collections or extra files) */}
                {(!isColorCollection ? files : nonColorFiles).map(file => (
                  <div
                    key={`file-${file.id}`}
                    className={`tree-node-row tree-leaf ${selectedAssetId === file.id ? 'selected' : ''}`}
                    onClick={() => onSelectAsset && onSelectAsset(file.id)}
                    onContextMenu={(e) => handleItemContextMenu(e, file, file.contentType)}
                  >
                    <span className="tree-expand-btn" />
                    <span className="tree-node-icon">{getAssetIcon(file)}</span>
                    <span className="tree-node-name" title={file.fileName}>{file.fileName}</span>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      )}

      {/* Context Menu */}
      {contextMenu && (
        <ContextMenu
          x={contextMenu.x}
          y={contextMenu.y}
          items={getContextMenuItems(contextMenu.item, contextMenu.type)}
          onClose={closeContextMenu}
        />
      )}
    </aside>
  );
}

function TreeSubCollection({ collection, expandedIds, onToggle, onClick, onContextMenu }) {
  const id = `col-${collection.id}`;
  const isExpanded = expandedIds.has(id);

  const typeIcon = (() => {
    switch(collection.type) {
      case 'image': return '🖼️';
      case 'link': return '🔗';
      case 'color': return '🎨';
      default: return '🗂️';
    }
  })();

  return (
    <div className="tree-node">
      <div
        className="tree-node-row"
        onClick={onClick}
        onContextMenu={onContextMenu}
      >
        <button
          className="tree-expand-btn"
          onClick={(e) => { e.stopPropagation(); onToggle(id); }}
        >
          ▸
        </button>
        <span className="tree-node-icon">{typeIcon}</span>
        <span className="tree-node-name">{collection.name}</span>
        {collection.type && (
          <span className="tree-node-badge">{collection.type}</span>
        )}
      </div>
    </div>
  );
}

function TreeFolder({ folder, collectionId, expandedIds, onToggle, selectedAssetId, onClick, onSelectAsset, onSelectFolder, onContextMenu, onItemContextMenu, getAssetIcon }) {
  const id = `folder-${folder.id}`;
  const isExpanded = expandedIds.has(id);
  const [children, setChildren] = useState(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (isExpanded && children === null && !loading) {
      setLoading(true);
      fetchCollectionItems(collectionId, folder.id)
        .then(data => {
          setChildren(data.items || []);
        })
        .catch(() => setChildren([]))
        .finally(() => setLoading(false));
    }
  }, [isExpanded, collectionId, folder.id, children, loading]);

  const childFolders = children ? children.filter(a => a.isFolder) : [];
  const childFiles = children ? children.filter(a => !a.isFolder) : [];

  return (
    <div className="tree-node">
      <div
        className="tree-node-row"
        onClick={onClick}
        onContextMenu={onContextMenu}
      >
        <button
          className="tree-expand-btn"
          onClick={(e) => { e.stopPropagation(); onToggle(id); }}
          title="Mở rộng"
        >
          {isExpanded ? '▾' : '▸'}
        </button>
        <span className="tree-node-icon">{isExpanded ? '📂' : '📁'}</span>
        <span className="tree-node-name" title={folder.fileName}>{folder.fileName}</span>
        {children && <span className="tree-node-count">{children.length}</span>}
      </div>
      {isExpanded && (
        <div className="tree-children">
          {loading && (
            <div className="tree-node-row tree-leaf">
              <span className="tree-expand-btn" />
              <span className="tree-node-name" style={{opacity: 0.5}}>Đang tải...</span>
            </div>
          )}
          {childFolders.map(subfolder => (
            <TreeFolder
              key={`folder-${subfolder.id}`}
              folder={subfolder}
              collectionId={collectionId}
              expandedIds={expandedIds}
              onToggle={onToggle}
              selectedAssetId={selectedAssetId}
              onClick={() => onSelectFolder && onSelectFolder(subfolder)}
              onSelectAsset={onSelectAsset}
              onSelectFolder={onSelectFolder}
              onContextMenu={(e) => onItemContextMenu && onItemContextMenu(e, subfolder, 'folder')}
              onItemContextMenu={onItemContextMenu}
              getAssetIcon={getAssetIcon}
            />
          ))}
          {childFiles.map(file => (
            <div
              key={`file-${file.id}`}
              className={`tree-node-row tree-leaf ${selectedAssetId === file.id ? 'selected' : ''}`}
              onClick={() => onSelectAsset && onSelectAsset(file.id)}
              onContextMenu={(e) => onItemContextMenu && onItemContextMenu(e, file, file.contentType)}
            >
              <span className="tree-expand-btn" />
              <span className="tree-node-icon">{getAssetIcon ? getAssetIcon(file) : '📄'}</span>
              <span className="tree-node-name" title={file.fileName}>{file.fileName}</span>
            </div>
          ))}
          {!loading && children && children.length === 0 && (
            <div className="tree-node-row tree-leaf">
              <span className="tree-expand-btn" />
              <span className="tree-node-name" style={{opacity: 0.5}}>(Trống)</span>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
