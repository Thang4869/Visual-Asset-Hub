import React, { useMemo, useState, useCallback, useRef } from 'react';
import ContextMenu from './ContextMenu';
import './ColorBoard.css';

const ColorBoard = ({
  items,
  onCreateColor,
  onCreateGroup,
  onSelectAsset,
  onMoveColorsToGroup,
  onMoveAsset,
  selectedAssetIds = new Set(),
  onCreateFolder,
  onOpenFolder,
  clipboard,
  onCopy,
  onCut,
  onPaste,
  onPinItem,
  onRenameAsset,
  onDeleteAsset,
  onDeleteFolder,
  refreshItems,
  onViewDetail,
  onUngroupColor,
  pinnedItems = [],
  showPrompt,
}) => {
  const [colorInput, setColorInput] = useState('');
  const [selectedGroupId, setSelectedGroupId] = useState('');
  const [dragItemIds, setDragItemIds] = useState(null); // array of ids being dragged
  const [dragGroupId, setDragGroupId] = useState(null); // group being dragged
  const [dropTarget, setDropTarget] = useState(null); // { groupId, insertBeforeId, position }
  const [groupDropTarget, setGroupDropTarget] = useState(null); // target group index for reorder
  const [copiedId, setCopiedId] = useState(null);
  const [contextMenu, setContextMenu] = useState(null);
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
    setDragGroupId(null);
    setGroupDropTarget(null);
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

  // ---- Group drag & drop (reorder groups freely) ----
  const handleGroupDragStart = useCallback((e, groupId) => {
    e.stopPropagation();
    setDragGroupId(groupId);
    e.dataTransfer.effectAllowed = 'move';
    e.dataTransfer.setData('text/group-id', String(groupId));
  }, []);

  const handleGroupDragOver = useCallback((e, targetGroupId) => {
    e.preventDefault();
    if (dragGroupId === null || dragGroupId === targetGroupId) return;
    e.dataTransfer.dropEffect = 'move';
    setGroupDropTarget(targetGroupId);
  }, [dragGroupId]);

  const handleGroupDrop = useCallback((e, targetGroupId) => {
    e.preventDefault();
    e.stopPropagation();
    if (dragGroupId === null || dragGroupId === targetGroupId) return;
    // Reorder groups by moving dragGroupId before targetGroupId
    if (onMoveColorsToGroup) {
      // Move the dragged group (as an asset) to reorder - we use sortOrder
      // For now, swap positions in the groups array
      const dragIdx = groups.findIndex(g => g.id === dragGroupId);
      const targetIdx = groups.findIndex(g => g.id === targetGroupId);
      if (dragIdx !== -1 && targetIdx !== -1) {
        const reorderedIds = groups.map(g => g.id);
        const [moved] = reorderedIds.splice(dragIdx, 1);
        reorderedIds.splice(targetIdx, 0, moved);
        // Use bulkMoveGroup or reorder API if available
        // For now we just trigger the move to signal reorder
      }
    }
    setDragGroupId(null);
    setGroupDropTarget(null);
  }, [dragGroupId, groups, onMoveColorsToGroup]);

  // ---- Context menu ----
  const handleContextMenu = useCallback((e, item, type) => {
    e.preventDefault();
    e.stopPropagation();
    setContextMenu({ x: e.clientX, y: e.clientY, item, type });
  }, []);

  const closeContextMenu = useCallback(() => setContextMenu(null), []);

  const getContextMenuItems = (item, type) => {
    const menuItems = [];

    menuItems.push({
      label: 'Ghim',
      icon: '📌',
      onClick: () => onPinItem && onPinItem(item, type),
    });

    if (type !== 'folder' && type !== 'color-group') {
      menuItems.push({
        label: 'Xem chi tiết',
        icon: '🔍',
        onClick: () => onViewDetail && onViewDetail(item),
      });
    }

    menuItems.push({ divider: true });

    if (type === 'color' && item.groupId) {
      menuItems.push({
        label: 'Bỏ nhóm',
        icon: '🔓',
        onClick: () => onUngroupColor && onUngroupColor(item),
      });
    }

    if (type === 'color') {
      menuItems.push({
        label: 'Sao chép mã màu',
        icon: '🎨',
        onClick: () => navigator.clipboard?.writeText(item.filePath || ''),
      });
    }

    menuItems.push({
      label: 'Sao chép đường dẫn',
      icon: '🔗',
      onClick: () => navigator.clipboard?.writeText(item.filePath || item.fileName || ''),
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
      onClick: () => onRenameAsset && onRenameAsset(item),
    });

    if (type === 'color-group' || type === 'folder') {
      menuItems.push({
        label: type === 'folder' ? 'Xóa thư mục' : 'Xóa nhóm',
        icon: '🗑️',
        onClick: () => {
          if (type === 'folder') {
            onDeleteFolder && onDeleteFolder(item.id);
          } else {
            onDeleteAsset && onDeleteAsset(item.id);
          }
        },
      });
    } else {
      menuItems.push({
        label: 'Xóa',
        icon: '🗑️',
        onClick: () => onDeleteAsset && onDeleteAsset(item.id),
      });
    }

    return menuItems;
  };

  // Separate folders from color items
  const pinnedIds = new Set(pinnedItems.map(p => p.item?.id).filter(Boolean));
  const folders = useMemo(
    () => {
      const f = items.filter(i => i.isFolder);
      f.sort((a, b) => (pinnedIds.has(a.id) ? 0 : 1) - (pinnedIds.has(b.id) ? 0 : 1));
      return f;
    },
    [items, pinnedItems]
  );

  return (
    <div
      className="color-board"
      onContextMenu={(e) => {
        // Right-click on empty area — show paste option
        if (e.target.closest('.color-item') || e.target.closest('.group-header') || e.target.closest('.color-folder-item')) return;
        e.preventDefault();
        const areaMenuItems = [];
        if (clipboard) {
          areaMenuItems.push({
            label: 'Dán vào đây',
            icon: '📥',
            shortcut: 'Ctrl+V',
            onClick: () => onPaste && onPaste({ id: null }, 'area'),
          });
        }
        areaMenuItems.push({
          label: 'Thêm màu mới',
          icon: '🎨',
          onClick: async () => {
            if (!showPrompt) return;
            const code = await showPrompt({ message: 'Nhập mã màu (vd: #FFAA00):', placeholder: '#FFAA00' });
            if (code) onCreateColor && onCreateColor(code.trim(), null);
          },
        });
        areaMenuItems.push({
          label: 'Tạo nhóm mới',
          icon: '🎯',
          onClick: () => onCreateGroup && onCreateGroup(),
        });
        if (onCreateFolder) {
          areaMenuItems.push({
            label: 'Tạo thư mục mới',
            icon: '📁',
            onClick: () => onCreateFolder(),
          });
        }
        setContextMenu({ x: e.clientX, y: e.clientY, item: null, type: 'area', menuItems: areaMenuItems });
      }}
    >
      <div className="color-toolbar">
        <div className="color-input-wrap">
          <input
            type="text"
            placeholder="Nhập mã màu (vd: #FFAA00) rồi Enter"
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
        <button className="group-btn" onClick={onCreateGroup}>Nhóm mới</button>
        {onCreateFolder && (
          <button className="group-btn" onClick={onCreateFolder}>Thư mục mới</button>
        )}
      </div>

      {/* Folders in color collection */}
      {folders.length > 0 && (
        <div className="color-folders-section">
          <h3 className="color-section-title">Thư mục</h3>
          <div className="color-folders-grid">
            {folders.map(folder => (
              <div
                key={folder.id}
                className={`color-folder-item ${selectedAssetIds.has(folder.id) ? 'multi-selected' : ''}`}
                onClick={(e) => {
                  if (e.ctrlKey || e.metaKey) {
                    onSelectAsset && onSelectAsset(folder.id, e);
                  } else {
                    onOpenFolder && onOpenFolder(folder);
                  }
                }}
                onContextMenu={(e) => handleContextMenu(e, folder, 'folder')}
                onDragOver={(e) => {
                  e.preventDefault();
                  e.currentTarget.classList.add('drag-over');
                }}
                onDragLeave={(e) => {
                  e.currentTarget.classList.remove('drag-over');
                }}
                onDrop={(e) => {
                  e.preventDefault();
                  e.currentTarget.classList.remove('drag-over');
                  // Handle color/group drop onto folder
                  let ids;
                  try { ids = JSON.parse(e.dataTransfer.getData('application/json')); } catch {}
                  const groupId = e.dataTransfer.getData('text/group-id');
                  if (ids && ids.length > 0 && onMoveAsset) {
                    ids.forEach(id => onMoveAsset(id, folder.id));
                  } else if (groupId && onMoveAsset) {
                    onMoveAsset(parseInt(groupId), folder.id);
                  }
                }}
              >
                <span className="color-folder-icon">📁</span>
                <span className="color-folder-name">{folder.fileName}</span>
              </div>
            ))}
          </div>
        </div>
      )}

      <div className="color-groups">
        {groupColumns.map((group, groupIdx) => {
          const groupColors = colors
            .filter(c => c.groupId === group.id)
            .sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0));
          const isOver = dropTarget?.groupId === group.id && dragItemIds !== null;
          const isGroupDropTarget = groupDropTarget === group.id && dragGroupId !== null;
          return (
            <div
              key={group.id ?? 'ungrouped'}
              className={[
                'color-group-column',
                group.id !== null && selectedAssetIds.has(group.id) ? 'multi-selected' : '',
                isOver ? 'drag-over' : '',
                isGroupDropTarget ? 'group-drag-over' : '',
                dragGroupId === group.id ? 'group-dragging' : '',
              ].filter(Boolean).join(' ')}
              draggable={group.id !== null}
              onDragStart={(e) => {
                // Only start group drag from the header
                if (group.id !== null && !dragItemIds) {
                  handleGroupDragStart(e, group.id);
                }
              }}
              onDragOver={(e) => {
                if (dragGroupId !== null) {
                  handleGroupDragOver(e, group.id);
                } else {
                  calcDropTarget(e, group.id, groupColors);
                }
              }}
              onDragLeave={handleDragLeaveGroup}
              onDrop={(e) => {
                if (dragGroupId !== null) {
                  handleGroupDrop(e, group.id);
                } else {
                  handleDrop(e, group.id);
                }
              }}
              onDragEnd={handleDragEnd}
            >
              <div
                className="group-header"
                onClick={(e) => {
                  e.stopPropagation();
                  if (group.id !== null) {
                    // Left click opens context menu for groups
                    handleContextMenu(e, group, 'color-group');
                  }
                }}
                onContextMenu={(e) => group.id !== null && handleContextMenu(e, group, 'color-group')}
                style={group.id !== null ? { cursor: 'grab' } : {}}
              >
                {group.id !== null && <span className="group-drag-handle">⠿</span>}
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
                        onClick={(e) => {
                          e.stopPropagation();
                          // Left click opens context menu for color items
                          handleContextMenu(e, color, 'color');
                        }}
                        onContextMenu={(e) => handleContextMenu(e, color, 'color')}
                      >
                        <span className="color-drag-handle" title="Kéo để sắp xếp">⠿</span>
                        <span
                          className="color-swatch"
                          style={{ backgroundColor: color.filePath }}
                        />
                        <span
                          className={`color-code ${copiedId === color.filePath ? 'copied' : ''}`}
                          onClick={(e) => { e.stopPropagation(); handleCopyCode(e, color.filePath); }}
                          title="Bấm để sao chép"
                        >
                          {copiedId === color.filePath ? '✓ Đã chép!' : color.filePath}
                        </span>
                      </div>
                    </React.Fragment>
                  );
                })}
                {showEndIndicator(group.id, groupColors) && <div className="drop-indicator" />}
                {groupColors.length === 0 && !showEndIndicator(group.id, groupColors) && (
                  <div className="group-empty">Chưa có màu</div>
                )}
              </div>
            </div>
          );
        })}
      </div>

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

export default ColorBoard;
