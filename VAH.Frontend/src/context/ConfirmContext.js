import React, { useState, useCallback, useRef, createContext, useContext } from 'react';
import ConfirmDialog from '../components/ConfirmDialog';

/**
 * ConfirmProvider — provides promise-based confirm/prompt/alert via context.
 *
 * Usage:
 *   const { confirm, prompt, alert } = useConfirm();
 *
 *   const ok = await confirm('Are you sure?');                         // true | false
 *   const ok = await confirm({ title, message, variant });             // true | false
 *   const val = await prompt({ message: ..., defaultValue: '...' });   // string | null
 *   await alert('Done!');                                              // void
 */
const ConfirmContext = createContext(null);

export function useConfirm() {
  const ctx = useContext(ConfirmContext);
  if (!ctx) throw new Error('useConfirm must be used within <ConfirmProvider>');
  return ctx;
}

export function ConfirmProvider({ children }) {
  const [state, setState] = useState(null);
  const resolveRef = useRef(null);

  // ── confirm(opts) → Promise<boolean> ──
  const confirm = useCallback((opts) => {
    const options = typeof opts === 'string' ? { message: opts } : opts;
    return new Promise((resolve) => {
      resolveRef.current = resolve;
      setState({
        mode: 'confirm',
        title: options.title || null,
        message: options.message || '',
        variant: options.variant || 'danger',
        confirmLabel: options.confirmLabel || 'Xác nhận',
        cancelLabel: options.cancelLabel || 'Hủy',
      });
    });
  }, []);

  // ── prompt(opts) → Promise<string|null> ──
  const prompt = useCallback((opts) => {
    const options = typeof opts === 'string' ? { message: opts } : opts;
    return new Promise((resolve) => {
      resolveRef.current = resolve;
      setState({
        mode: 'prompt',
        title: options.title || null,
        message: options.message || '',
        variant: options.variant || 'info',
        confirmLabel: options.confirmLabel || 'OK',
        cancelLabel: options.cancelLabel || 'Hủy',
        placeholder: options.placeholder || '',
        defaultValue: options.defaultValue || '',
        inputType: options.inputType || 'text',
        selectOptions: options.selectOptions || [],
      });
    });
  }, []);

  // ── alert(opts) → Promise<void> ──
  const alertFn = useCallback((opts) => {
    const options = typeof opts === 'string' ? { message: opts } : opts;
    return new Promise((resolve) => {
      resolveRef.current = resolve;
      setState({
        mode: 'alert',
        title: options.title || null,
        message: options.message || '',
        variant: options.variant || 'info',
        confirmLabel: options.confirmLabel || 'OK',
      });
    });
  }, []);

  const handleConfirm = useCallback((value) => {
    if (state?.mode === 'prompt') {
      resolveRef.current?.(value);        // string from input
    } else if (state?.mode === 'alert') {
      resolveRef.current?.();             // void
    } else {
      resolveRef.current?.(true);         // boolean
    }
    setState(null);
  }, [state?.mode]);

  const handleCancel = useCallback(() => {
    if (state?.mode === 'prompt') {
      resolveRef.current?.(null);         // null = cancelled
    } else {
      resolveRef.current?.(false);        // boolean
    }
    setState(null);
  }, [state?.mode]);

  const ctxValue = React.useMemo(() => ({ confirm, prompt, alert: alertFn }), [confirm, prompt, alertFn]);

  return React.createElement(
    ConfirmContext.Provider,
    { value: ctxValue },
    children,
    React.createElement(ConfirmDialog, {
      open: !!state,
      mode: state?.mode || 'confirm',
      title: state?.title,
      message: state?.message || '',
      variant: state?.variant,
      confirmLabel: state?.confirmLabel,
      cancelLabel: state?.cancelLabel,
      placeholder: state?.placeholder,
      defaultValue: state?.defaultValue,
      inputType: state?.inputType,
      selectOptions: state?.selectOptions,
      onConfirm: handleConfirm,
      onCancel: handleCancel,
    })
  );
}
