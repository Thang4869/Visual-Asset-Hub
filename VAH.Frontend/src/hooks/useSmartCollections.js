import { useState, useEffect, useCallback } from 'react';
import * as smartCollectionsApi from '../api/smartCollectionsApi';

/**
 * Hook encapsulating smart collection state and actions.
 */
export default function useSmartCollections(isAuthenticated) {
  const [smartCollections, setSmartCollections] = useState([]);
  const [activeSmartCollection, setActiveSmartCollection] = useState(null);
  const [smartItems, setSmartItems] = useState([]);

  const fetchSmartCollections = useCallback(async () => {
    try {
      const defs = await smartCollectionsApi.fetchSmartCollections();
      setSmartCollections(defs);
    } catch (e) { console.error('Smart collections error:', e); }
  }, []);

  useEffect(() => {
    if (isAuthenticated) fetchSmartCollections();
  }, [isAuthenticated, fetchSmartCollections]);

  const handleSelectSmartCollection = useCallback(async (sc) => {
    setActiveSmartCollection(sc);
    try {
      const result = await smartCollectionsApi.fetchSmartCollectionItems(sc.id);
      setSmartItems(result.items || []);
    } catch (e) { console.error('Smart collection items error:', e); }
  }, []);

  const clearSmartCollection = useCallback(() => {
    setActiveSmartCollection(null);
    setSmartItems([]);
  }, []);

  return {
    smartCollections,
    activeSmartCollection,
    smartItems,
    handleSelectSmartCollection,
    clearSmartCollection,
  };
}
