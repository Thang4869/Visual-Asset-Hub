import React from 'react';
import CollectionTree from './CollectionTree';

/**
 * Left sidebar — collection tree + smart collections list.
 */
export default function AppSidebar({
  collections,
  selectedCollection,
  smartCollections,
  activeSmartCollection,
  onSelectCollection,
  onCreateCollection,
  onDeleteCollection,
  onSelectSmartCollection,
  onAddCollection,
  pinnedItems,
  onPinItem,
  onNavigateToPinned,
}) {
  return (
    <aside className="app-sidebar">
      <div className="sidebar-header">
        <h3>Tài liệu của tôi</h3>
        <button className="add-collection-btn" onClick={() => {
          if (onAddCollection) onAddCollection();
          else onCreateCollection && onCreateCollection('Untitled');
        }}>+</button>
      </div>
      <div className="sidebar-scroll">
        <CollectionTree
          collections={collections}
          selectedCollection={selectedCollection}
          onSelectCollection={(c) => onSelectCollection(c, [c])}
          onCreateCollection={onCreateCollection}
          onDeleteCollection={onDeleteCollection}
        />

        {/* Smart Collections */}
        {smartCollections.length > 0 && (
          <div className="smart-collections-section">
            <h4 className="sidebar-section-title">Bộ sưu tập thông minh</h4>
            {smartCollections.map((sc) => (
              <button
                key={sc.id}
                className={`smart-collection-item ${activeSmartCollection?.id === sc.id ? 'active' : ''}`}
                onClick={() => {
                  onSelectCollection(null, []);
                  onSelectSmartCollection(sc);
                }}
              >
                <span className="sc-icon">{sc.icon}</span>
                <span className="sc-name">{sc.name}</span>
                <span className="sc-count">{sc.count}</span>
              </button>
            ))}
          </div>
        )}

        {/* Pinned Items */}
        {pinnedItems && pinnedItems.length > 0 && (
          <div className="pinned-section">
            <h4 className="sidebar-section-title">📌 Đã ghim</h4>
            {pinnedItems.map(({ item, type }) => (
              <div
                key={`${type}-${item.id}`}
                className="pinned-item clickable"
                title={item.fileName || item.name || ''}
                onClick={() => onNavigateToPinned && onNavigateToPinned(item, type)}
              >
                <span className="pinned-icon">
                  {type === 'folder' ? '📁' : type === 'color' ? '🎨' : type === 'link' ? '🔗' : type === 'image' ? '🖼️' : type === 'collection' ? '🗂️' : '📄'}
                </span>
                <span className="pinned-name">{item.fileName || item.name || item.filePath || ''}</span>
                <button
                  className="pinned-remove"
                  onClick={(e) => { e.stopPropagation(); onPinItem && onPinItem(item, type); }}
                  title="Bỏ ghim"
                >
                  ×
                </button>
              </div>
            ))}
          </div>
        )}
      </div>
    </aside>
  );
}
