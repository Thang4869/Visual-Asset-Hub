import React, { useMemo, useState, useCallback, useRef } from 'react';
import './ColorBoard.css';

const ColorBoard = ({ items, onCreateColor, onCreateGroup, onSelectAsset, onMoveColorsToGroup, selectedAssetIds = new Set() }) => {
  const [colorInput, setColorInput] = useState('');
  const [selectedGroupId, setSelectedGroupId] = useState('');
  const [dragItemIds, setDragItemIds] = useState(null); // array of ids being dragged
  const [dropTarget, setDropTarget] = useState(null); // { groupId, insertBeforeId, position }
  const [copiedId, setCopiedId] = useState(null);
  const copyTimeoutRef = useRef(null);

  const groups = useMemo(
    () => items.filter(i => i.contentType === 'color-group'),
    [items]
  );

  const colors = useMemo(
    () => items.filter(i => i.contentType === 'color'),
    [items]
  );

  const groupOptions = [{ id: '', fileName: 'Ungrouped' }, ...groups];

  const handleKeyDown = (e) => {
    if (e.key === 'Enter' && colorInput.trim()) {
      let code = colorInput.trim();
      if (!code.startsWith('#') && /^[0-9A-Fa-f]{3,8}$/.test(code)) {
        code = '#' + code;
      }
      const groupId = selectedGroupId ? parseInt(selectedGroupId, 10) : null;
      onCreateColor(code, groupId);
      setColorInput('');
    }
  };

  const groupColumns = [{ id: null, fileName: 'Ungrouped' }, ...groups];

  // ---- Copy color code ----
  const handleCopyCode = useCallback((e, colorCode) => {
    e.stopPropagation();
    navigator.clipboard.writeText(colorCode).then(() => {
      setCopiedId(colorCode);
      if (copyTimeoutRef.current) clearTimeout(copyTimeoutRef.current);
      copyTimeoutRef.current = setTimeout(() => setCopiedId(null), 1500);
    }).catch(() => {});
  }, []);

  // ---- Drag & Drop handlers ----
  const handleDragStart = useCallback((e, colorId) => {
    // If the dragged item is part of selection, drag all selected colors
    let ids;
    if (selectedAssetIds.has(colorId) && selectedAssetIds.size > 1) {
      // Only include colors (not groups) from selection
      ids = Array.from(selectedAssetIds).filter(id => colors.some(c => c.id === id));
    } else {
      ids = [colorId];
    }
    setDragItemIds(ids);
    e.dataTransfer.effectAllowed = 'move';
    e.dataTransfer.setData('application/json', JSON.stringify(ids));

    // Custom drag image showing count
    if (ids.length > 1) {
      const badge = document.createElement('div');
      badge.textContent = `${ids.length} colors`;
      badge.style.cssText = 'position:fixed;top:-100px;background:#2196F3;color:#fff;padding:6px 12px;border-radius:6px;font-size:13px;font-weight:600;pointer-events:none;';
      document.body.appendChild(badge);
      e.dataTransfer.setDragImage(badge, 40, 16);
      requestAnimationFrame(() => document.body.removeChild(badge));
    }
  }, [selectedAssetIds, colors]);

  const handleDragEnd = useCallback(() => {
    setDragItemIds(null);
    setDropTarget(null);
  }, []);

  // Calculate drop position within a group's item list
  const calcDropTarget = useCallback((e, groupId, groupColors) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';

    // Find the closest color-item element under cursor
    const items = e.currentTarget.querySelectorAll('.color-item');
    let insertBeforeId = null;
    let position = 'end'; // 'before', 'after', 'end'

    for (const item of items) {
      const rect = item.getBoundingClientRect();
      const midY = rect.top + rect.height / 2;
      const itemId = parseInt(item.dataset.colorId, 10);
      if (e.clientY < midY) {
        insertBeforeId = itemId;
        position = 'before';
        break;
      }
    }

    setDropTarget({ groupId, insertBeforeId, position });
  }, []);

  const handleDragLeaveGroup = useCallback((e) => {
    if (!e.currentTarget.contains(e.relatedTarget)) {
      setDropTarget(null);
    }
  }, []);

  const handleDrop = useCallback((e, targetGroupId) => {
    e.preventDefault();
    if (!onMoveColorsToGroup) return;

    let ids;
    try {
      ids = JSON.parse(e.dataTransfer.getData('application/json'));
    } catch {
      return;
    }
    if (!ids || ids.length === 0) return;

    const insertBeforeId = dropTarget?.insertBeforeId ?? null;

    onMoveColorsToGroup(ids, targetGroupId, insertBeforeId);
    setDragItemIds(null);
    setDropTarget(null);
  }, [dropTarget, onMoveColorsToGroup]);

  // Check if a drop indicator should show before/after a specific color
  const getDropIndicator = (colorId, groupId) => {
    if (!dropTarget || !dragItemIds) return null;
    if (dropTarget.groupId !== groupId) return null;
    if (dropTarget.insertBeforeId === colorId) return 'before';
    return null;
  };

  const showEndIndicator = (groupId, groupColors) => {
    if (!dropTarget || !dragItemIds) return false;
    return dropTarget.groupId === groupId && dropTarget.insertBeforeId === null;
  };

  return (
    <div className="color-board">
      <div className="color-toolbar">
        <div className="color-input-wrap">
          <input
            type="text"
            placeholder="Enter color code (e.g. #FFAA00) and press Enter"
            value={colorInput}
            onChange={(e) => setColorInput(e.target.value)}
            onKeyDown={handleKeyDown}
          />
          <select
            value={selectedGroupId}
            onChange={(e) => setSelectedGroupId(e.target.value)}
          >
            {groupOptions.map(g => (
              <option key={g.id ?? 'ungrouped'} value={g.id ?? ''}>
                {g.fileName}
              </option>
            ))}
          </select>
        </div>
        <button className="group-btn" onClick={onCreateGroup}>New group</button>
      </div>

      <div className="color-groups">
        {groupColumns.map(group => {
          const groupColors = colors
            .filter(c => c.groupId === group.id)
            .sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0));
          const isOver = dropTarget?.groupId === group.id && dragItemIds !== null;
          return (
            <div
              key={group.id ?? 'ungrouped'}
              className={[
                'color-group-column',
                group.id !== null && selectedAssetIds.has(group.id) ? 'multi-selected' : '',
                isOver ? 'drag-over' : '',
              ].filter(Boolean).join(' ')}
              onDragOver={(e) => calcDropTarget(e, group.id, groupColors)}
              onDragLeave={handleDragLeaveGroup}
              onDrop={(e) => handleDrop(e, group.id)}
            >
              <div
                className="group-header"
                onClick={(e) => group.id !== null && onSelectAsset && onSelectAsset(group.id, e)}
                style={group.id !== null ? { cursor: 'pointer' } : {}}
              >
                <span className="group-title">{group.fileName}</span>
                <span className="group-count">{groupColors.length}</span>
              </div>
              <div className="group-items">
                {groupColors.map(color => {
                  const indicator = getDropIndicator(color.id, group.id);
                  const isDragging = dragItemIds && dragItemIds.includes(color.id);
                  return (
                    <React.Fragment key={color.id}>
                      {indicator === 'before' && <div className="drop-indicator" />}
                      <div
                        data-color-id={color.id}
                        className={[
                          'color-item',
                          selectedAssetIds.has(color.id) ? 'multi-selected' : '',
                          isDragging ? 'dragging' : '',
                        ].filter(Boolean).join(' ')}
                        draggable
                        onDragStart={(e) => handleDragStart(e, color.id)}
                        onDragEnd={handleDragEnd}
                        onClick={(e) => onSelectAsset && onSelectAsset(color.id, e)}
                      >
                        <span className="color-drag-handle" title="Drag to reorder">⠿</span>
                        <span
                          className="color-swatch"
                          style={{ backgroundColor: color.filePath }}
                        />
                        <span
                          className={`color-code ${copiedId === color.filePath ? 'copied' : ''}`}
                          onClick={(e) => handleCopyCode(e, color.filePath)}
                          title="Click to copy"
                        >
                          {copiedId === color.filePath ? '✓ Copied!' : color.filePath}
                        </span>
                      </div>
                    </React.Fragment>
                  );
                })}
                {showEndIndicator(group.id, groupColors) && <div className="drop-indicator" />}
                {groupColors.length === 0 && !showEndIndicator(group.id, groupColors) && (
                  <div className="group-empty">No colors</div>
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};

export default ColorBoard;
