import { useState, useCallback, useEffect } from 'react';
import * as tagsApi from '../api/tagsApi';

/**
 * Hook encapsulating tag operations.
 */
export default function useTags() {
  const [tags, setTags] = useState([]);
  const [loading, setLoading] = useState(false);

  const fetchTags = useCallback(async () => {
    setLoading(true);
    try {
      const data = await tagsApi.fetchAllTags();
      setTags(data);
    } catch (err) {
      console.error('Error fetching tags:', err);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchTags();
  }, [fetchTags]);

  const createTag = useCallback(async (name, color) => {
    try {
      const tag = await tagsApi.createTag({ name, color });
      setTags((prev) => [...prev.filter(t => t.id !== tag.id), tag].sort((a, b) => a.name.localeCompare(b.name)));
      return tag;
    } catch (err) {
      console.error('Error creating tag:', err);
      throw err;
    }
  }, []);

  const updateTag = useCallback(async (id, payload) => {
    try {
      const updated = await tagsApi.updateTag(id, payload);
      setTags((prev) => prev.map(t => t.id === id ? updated : t));
      return updated;
    } catch (err) {
      console.error('Error updating tag:', err);
      throw err;
    }
  }, []);

  const deleteTag = useCallback(async (id) => {
    try {
      await tagsApi.deleteTag(id);
      setTags((prev) => prev.filter(t => t.id !== id));
    } catch (err) {
      console.error('Error deleting tag:', err);
      throw err;
    }
  }, []);

  const getAssetTags = useCallback(async (assetId) => {
    try {
      return await tagsApi.getAssetTags(assetId);
    } catch (err) {
      console.error('Error getting asset tags:', err);
      return [];
    }
  }, []);

  const setAssetTags = useCallback(async (assetId, tagIds) => {
    try {
      await tagsApi.setAssetTags(assetId, tagIds);
    } catch (err) {
      console.error('Error setting asset tags:', err);
      throw err;
    }
  }, []);

  const addAssetTags = useCallback(async (assetId, tagIds) => {
    try {
      await tagsApi.addAssetTags(assetId, tagIds);
    } catch (err) {
      console.error('Error adding asset tags:', err);
      throw err;
    }
  }, []);

  const removeAssetTags = useCallback(async (assetId, tagIds) => {
    try {
      await tagsApi.removeAssetTags(assetId, tagIds);
    } catch (err) {
      console.error('Error removing asset tags:', err);
      throw err;
    }
  }, []);

  return {
    tags,
    loading,
    fetchTags,
    createTag,
    updateTag,
    deleteTag,
    getAssetTags,
    setAssetTags,
    addAssetTags,
    removeAssetTags,
  };
}
