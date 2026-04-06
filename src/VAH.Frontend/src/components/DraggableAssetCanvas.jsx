import React, { useState, useRef } from 'react';
import { staticUrl } from '../api/client';
import { updatePosition } from '../api/assetsApi';
import './DraggableAssetCanvas.css';

const DraggableAssetCanvas = ({ assets, onPositionUpdate }) => {
  const [draggedAsset, setDraggedAsset] = useState(null);
  const [offset, setOffset] = useState({ x: 0, y: 0 });
  const canvasRef = useRef(null);

  const handleMouseDown = (e, asset) => {
    if (e.button !== 0) return; // Chỉ xử lý left click

    const canvas = canvasRef.current;
    const rect = canvas.getBoundingClientRect();
    const mouseX = e.clientX - rect.left;
    const mouseY = e.clientY - rect.top;

    setDraggedAsset(asset);
    setOffset({
      x: mouseX - asset.positionX,
      y: mouseY - asset.positionY,
    });
  };

  const handleMouseMove = (e) => {
    if (!draggedAsset || !canvasRef.current) return;

    const canvas = canvasRef.current;
    const rect = canvas.getBoundingClientRect();
    const mouseX = e.clientX - rect.left;
    const mouseY = e.clientY - rect.top;

    const newX = Math.max(0, Math.min(mouseX - offset.x, rect.width - 150));
    const newY = Math.max(0, Math.min(mouseY - offset.y, rect.height - 150));

    // Cập nhật vị trí trong state (không thay đổi asset ban đầu)
    const updatedAssets = assets.map(a =>
      a.id === draggedAsset.id
        ? { ...a, positionX: newX, positionY: newY }
        : a
    );

    // Cập nhật draggedAsset để hiển thị vị trí mới
    setDraggedAsset(prev => ({
      ...prev,
      positionX: newX,
      positionY: newY,
    }));

    // Gọi callback để cập nhật assets trong App.jsx
    if (onPositionUpdate) {
      onPositionUpdate(updatedAssets);
    }
  };

  const handleMouseUp = async () => {
    if (!draggedAsset) return;

    // Lưu vị trí mới lên backend
    try {
      await updatePosition(
        draggedAsset.id,
        draggedAsset.positionX,
        draggedAsset.positionY,
      );
    } catch (error) {
      console.error('Error saving position:', error);
    }

    setDraggedAsset(null);
  };

  return (
    <div
      ref={canvasRef}
      className="draggable-canvas"
      onMouseMove={handleMouseMove}
      onMouseUp={handleMouseUp}
      onMouseLeave={handleMouseUp}
    >
      {assets.map(asset => (
        <div
          key={asset.id}
          className="draggable-asset"
          style={{
            left: `${asset.positionX}px`,
            top: `${asset.positionY}px`,
            cursor: draggedAsset?.id === asset.id ? 'grabbing' : 'grab',
          }}
          onMouseDown={(e) => handleMouseDown(e, asset)}
        >
          <img
            src={staticUrl(asset.filePath)}
            alt={asset.fileName}
            className="draggable-image"
            draggable={false}
          />
          <div className="asset-label">{asset.fileName}</div>
        </div>
      ))}
    </div>
  );
};

export default DraggableAssetCanvas;
