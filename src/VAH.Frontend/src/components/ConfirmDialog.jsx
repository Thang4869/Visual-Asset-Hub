import React, { useEffect, useRef, useState } from 'react';
import './ConfirmDialog.css';

/**
 * Unified styled dialog replacing window.confirm(), window.prompt(), and window.alert().
 *
 * Modes (via `mode` prop):
 * - 'confirm' : message + OK/Cancel → onConfirm() or onCancel()
 * - 'prompt'  : message + input + OK/Cancel → onConfirm(inputValue) or onCancel()
 * - 'alert'   : message + OK → onConfirm()
 */
export default function ConfirmDialog({
  open,
  mode = 'confirm',
  title,
  message,
  placeholder = '',
  defaultValue = '',
  confirmLabel = 'Xác nhận',
  cancelLabel = 'Hủy',
  variant = 'danger',
  inputType = 'text',
  selectOptions = [],
  onConfirm,
  onCancel,
}) {
  const dialogRef = useRef(null);
  const inputRef = useRef(null);
  const [inputValue, setInputValue] = useState(defaultValue);

  // Reset input value whenever the dialog opens with a new default
  useEffect(() => {
    if (open) setInputValue(defaultValue);
  }, [open, defaultValue]);

  useEffect(() => {
    if (!open) return;
    const handleKey = (e) => {
      if (e.key === 'Escape') {
        if (mode === 'alert') onConfirm?.();
        else onCancel?.();
      }
      // Enter submits for confirm/alert (prompt handles Enter in input)
      if (e.key === 'Enter' && mode !== 'prompt') {
        onConfirm?.();
      }
    };
    window.addEventListener('keydown', handleKey);
    return () => window.removeEventListener('keydown', handleKey);
  }, [open, onCancel, onConfirm, mode]);

  // Focus input in prompt mode, or dialog otherwise
  useEffect(() => {
    if (open) {
      if (mode === 'prompt' && inputRef.current) {
        setTimeout(() => {
          inputRef.current.focus();
          inputRef.current.select?.();
        }, 50);
      } else if (dialogRef.current) {
        dialogRef.current.focus();
      }
    }
  }, [open, mode]);

  if (!open) return null;

  const handleSubmit = () => {
    if (mode === 'prompt') onConfirm?.(inputValue);
    else onConfirm?.();
  };

  const handleInputKeyDown = (e) => {
    if (e.key === 'Enter') { e.preventDefault(); handleSubmit(); }
    if (e.key === 'Escape') { e.preventDefault(); onCancel?.(); }
  };

  const showCancel = mode !== 'alert';

  return (
    <div className="confirm-overlay" onClick={showCancel ? onCancel : undefined}>
      <div
        ref={dialogRef}
        className={`confirm-dialog confirm-${variant}`}
        onClick={(e) => e.stopPropagation()}
        tabIndex={-1}
      >
        {title && <h3 className="confirm-title">{title}</h3>}
        {message && <p className="confirm-message">{message}</p>}

        {mode === 'prompt' && (
          <div className="confirm-input-wrap">
            {inputType === 'select' ? (
              <select
                ref={inputRef}
                className="confirm-select"
                value={inputValue}
                onChange={(e) => setInputValue(e.target.value)}
                onKeyDown={handleInputKeyDown}
              >
                {selectOptions.map(opt => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
            ) : (
              <input
                ref={inputRef}
                className="confirm-input"
                type="text"
                placeholder={placeholder}
                value={inputValue}
                onChange={(e) => setInputValue(e.target.value)}
                onKeyDown={handleInputKeyDown}
              />
            )}
          </div>
        )}

        <div className="confirm-actions">
          {showCancel && (
            <button className="confirm-btn-cancel" onClick={onCancel}>
              {cancelLabel}
            </button>
          )}
          <button className={`confirm-btn-ok confirm-btn-${variant}`} onClick={handleSubmit}>
            {confirmLabel}
          </button>
        </div>
      </div>
    </div>
  );
}
