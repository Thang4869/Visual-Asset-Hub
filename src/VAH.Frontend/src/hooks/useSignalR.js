import { useEffect, useRef, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';
import { getToken, API_BASE_URL } from '../api/client';

const HUB_URL = API_BASE_URL.replace(/\/api(?:\/v\d+)?$/i, '') + '/hubs/assets';

/**
 * Hook for real-time SignalR notifications.
 *
 * @param {Object} handlers - Map of event names to handler functions.
 *   e.g. { AssetCreated: (data) => ..., CollectionDeleted: (data) => ... }
 * @param {boolean} enabled - Whether the connection should be active.
 */
export default function useSignalR(handlers = {}, enabled = true) {
  const connectionRef = useRef(null);
  const handlersRef = useRef(handlers);
  handlersRef.current = handlers;

  const connect = useCallback(async () => {
    if (connectionRef.current) return;

    const token = getToken();
    if (!token) return;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, {
        accessTokenFactory: () => getToken(),
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    // Subscribe to all known events
    const events = [
      'AssetCreated', 'AssetUpdated', 'AssetDeleted',
      'AssetsUploaded', 'AssetsBulkDeleted', 'AssetsBulkMoved',
      'CollectionCreated', 'CollectionUpdated', 'CollectionDeleted',
      'TagsChanged',
    ];

    events.forEach((event) => {
      connection.on(event, (data) => {
        if (handlersRef.current[event]) {
          handlersRef.current[event](data);
        }
      });
    });

    connection.onreconnecting(() => {
      console.log('[SignalR] Reconnecting...');
    });

    connection.onreconnected(() => {
      console.log('[SignalR] Reconnected');
    });

    connection.onclose((err) => {
      console.log('[SignalR] Connection closed', err);
      connectionRef.current = null;
    });

    try {
      await connection.start();
      connectionRef.current = connection;
      console.log('[SignalR] Connected');
    } catch (err) {
      console.warn('[SignalR] Connection failed:', err);
      connectionRef.current = null;
    }
  }, []);

  const disconnect = useCallback(async () => {
    if (connectionRef.current) {
      try {
        await connectionRef.current.stop();
      } catch (e) {
        // ignore
      }
      connectionRef.current = null;
    }
  }, []);

  useEffect(() => {
    if (enabled) {
      connect();
    } else {
      disconnect();
    }

    return () => { disconnect(); };
  }, [enabled, connect, disconnect]);

  return { connection: connectionRef.current };
}
