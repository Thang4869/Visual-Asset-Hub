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
}) {
  return (
    <aside className="app-sidebar">
      <div className="sidebar-header">
        <h3>Tài liệu của tôi</h3>
        <button className="add-collection-btn" onClick={() => {
          const name = prompt('Tên collection:');
          if (name) onCreateCollection(name);
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
      </div>
    </aside>
  );
}
