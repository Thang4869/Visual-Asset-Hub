import React, { useState } from 'react';
import './CollectionTree.css';

const CollectionTree = ({ 
  collections, 
  selectedCollection, 
  onSelectCollection, 
  onCreateCollection,
  onDeleteCollection 
}) => {
  const [expandedIds, setExpandedIds] = useState(new Set());

  const toggleExpand = (id) => {
    const newExpanded = new Set(expandedIds);
    if (newExpanded.has(id)) {
      newExpanded.delete(id);
    } else {
      newExpanded.add(id);
    }
    setExpandedIds(newExpanded);
  };

  // Lấy các collection gốc (parent = null)
  const rootCollections = collections.filter(c => !c.parentId);

  // Lấy các collection con của một parent
  const getChildCollections = (parentId) => {
    return collections.filter(c => c.parentId === parentId);
  };

  const renderCollectionItem = (collection, level = 0) => {
    const childCollections = getChildCollections(collection.id);
    const hasChildren = childCollections.length > 0;
    const isExpanded = expandedIds.has(collection.id);
    const isSelected = selectedCollection?.id === collection.id;

    return (
      <div key={collection.id} className="collection-item" style={{ paddingLeft: `${level * 16}px` }}>
        <div className="collection-row">
          {hasChildren && (
            <button
              className="expand-btn"
              onClick={() => toggleExpand(collection.id)}
            >
              {isExpanded ? '▼' : '▶'}
            </button>
          )}
          {!hasChildren && <span className="expand-placeholder" />}

          <div
            className={`collection-name ${isSelected ? 'selected' : ''}`}
            onClick={() => onSelectCollection(collection)}
            style={{ borderLeftColor: collection.color }}
          >
            {collection.type === 'image' && <span className="icon">🖼️</span>}
            {collection.type === 'link' && <span className="icon">🔗</span>}
            {collection.type === 'color' && <span className="icon">🎨</span>}
            {collection.type === 'default' && <span className="icon">📁</span>}
            <span>{collection.name}</span>
          </div>

          <button
            className="delete-btn"
            onClick={(e) => {
              e.stopPropagation();
              onDeleteCollection(collection.id);
            }}
            title="Xóa collection"
          >
            ✕
          </button>
        </div>

        {isExpanded && hasChildren && (
          <div className="subcollections">
            {childCollections.map(child => renderCollectionItem(child, level + 1))}
          </div>
        )}
      </div>
    );
  };

  return (
    <div className="collection-tree">
      <div className="collections-list">
        {rootCollections.length > 0 ? (
          rootCollections.map(collection => renderCollectionItem(collection))
        ) : (
          <p className="empty-message">No collections</p>
        )}
      </div>
    </div>
  );
};

export default CollectionTree;
