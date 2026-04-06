import { useState, useCallback, useRef } from 'react';

/**
 * Generic undo/redo hook using a command pattern.
 *
 * Each "command" is an object with:
 *   { execute: async () => ..., undo: async () => ..., description: string }
 *
 * Usage:
 *   const { execute, undo, redo, canUndo, canRedo, history } = useUndoRedo();
 *   await execute({ execute: () => deleteAsset(id), undo: () => restoreAsset(data), description: 'Delete asset' });
 *   // User presses Ctrl+Z → call undo()
 *   // User presses Ctrl+Shift+Z → call redo()
 */
export default function useUndoRedo(maxHistory = 50) {
  const [undoStack, setUndoStack] = useState([]);
  const [redoStack, setRedoStack] = useState([]);
  const isExecuting = useRef(false);

  /**
   * Execute a new command and push it to the undo stack.
   */
  const execute = useCallback(async (command) => {
    if (isExecuting.current) return;
    isExecuting.current = true;

    try {
      await command.execute();
      setUndoStack((prev) => {
        const next = [...prev, { ...command, timestamp: Date.now() }];
        return next.length > maxHistory ? next.slice(-maxHistory) : next;
      });
      setRedoStack([]); // Clear redo on new action
    } catch (err) {
      console.error('Command execution failed:', err);
      throw err;
    } finally {
      isExecuting.current = false;
    }
  }, [maxHistory]);

  /**
   * Undo the last command.
   */
  const undo = useCallback(async () => {
    if (isExecuting.current) return;

    setUndoStack((prev) => {
      if (prev.length === 0) return prev;
      const command = prev[prev.length - 1];
      const next = prev.slice(0, -1);

      isExecuting.current = true;
      command.undo().then(() => {
        setRedoStack((r) => [...r, command]);
      }).catch((err) => {
        console.error('Undo failed:', err);
        // Push back the command if undo fails
        setUndoStack((s) => [...s, command]);
      }).finally(() => {
        isExecuting.current = false;
      });

      return next;
    });
  }, []);

  /**
   * Redo the last undone command.
   */
  const redo = useCallback(async () => {
    if (isExecuting.current) return;

    setRedoStack((prev) => {
      if (prev.length === 0) return prev;
      const command = prev[prev.length - 1];
      const next = prev.slice(0, -1);

      isExecuting.current = true;
      command.execute().then(() => {
        setUndoStack((u) => [...u, command]);
      }).catch((err) => {
        console.error('Redo failed:', err);
        setRedoStack((r) => [...r, command]);
      }).finally(() => {
        isExecuting.current = false;
      });

      return next;
    });
  }, []);

  const canUndo = undoStack.length > 0;
  const canRedo = redoStack.length > 0;

  // Recent history for display
  const history = undoStack.slice(-10).reverse().map((cmd) => ({
    description: cmd.description,
    timestamp: cmd.timestamp,
  }));

  return { execute, undo, redo, canUndo, canRedo, history };
}
