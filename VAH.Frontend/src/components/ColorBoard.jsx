import React, { useMemo, useState } from 'react';
import './ColorBoard.css';

const ColorBoard = ({ items, onCreateColor, onCreateGroup }) => {
  const [colorInput, setColorInput] = useState('');
  const [selectedGroupId, setSelectedGroupId] = useState('');

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
      const groupId = selectedGroupId ? parseInt(selectedGroupId, 10) : null;
      onCreateColor(colorInput.trim(), groupId);
      setColorInput('');
    }
  };

  const groupColumns = [{ id: null, fileName: 'Ungrouped' }, ...groups];

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
          const groupColors = colors.filter(c => c.groupId === group.id);
          return (
            <div key={group.id ?? 'ungrouped'} className="color-group-column">
              <div className="group-header">
                <span className="group-title">{group.fileName}</span>
                <span className="group-count">{groupColors.length}</span>
              </div>
              <div className="group-items">
                {groupColors.map(color => (
                  <div key={color.id} className="color-item">
                    <span
                      className="color-swatch"
                      style={{ backgroundColor: color.filePath }}
                    />
                    <span className="color-code">{color.filePath}</span>
                  </div>
                ))}
                {groupColors.length === 0 && (
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
