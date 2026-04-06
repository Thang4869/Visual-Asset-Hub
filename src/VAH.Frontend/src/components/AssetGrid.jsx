import React from 'react';
import './AssetGrid.css';

const AssetGrid = ({ assets }) => {
  return (
    <div className="asset-grid">
      {assets.map(asset => (
        <div key={asset.id} className="asset-card">
          <img src={asset.thumbnailUrl} alt={asset.fileName} className="asset-thumbnail" />
          <div className="asset-info">
            <p className="asset-name">{asset.fileName}</p>
            <p className="asset-tags">
              {Array.isArray(asset.tags) ? asset.tags.join(', ') : asset.tags}
            </p>
          </div>
        </div>
      ))}
    </div>
  );
};

export default AssetGrid;