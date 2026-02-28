import axios from 'axios';
import tokenManager from './TokenManager';

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5027/api',
  timeout: 30000,
  headers: {
    'Accept': 'application/json',
  },
});

// ---- Backward-compatible token helpers (delegate to TokenManager) ----
export const getToken = () => tokenManager.getToken();
export const setToken = (token) => tokenManager.setToken(token);
export const clearToken = () => tokenManager.clearToken();

// Request interceptor — attach JWT bearer token
apiClient.interceptors.request.use(
  (config) => {
    const token = tokenManager.getToken();
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error),
);

// Response interceptor — normalise errors & handle 401
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response) {
      const { status, data } = error.response;
      const message = data?.detail || data?.title || error.message;
      console.error(`[API ${status}]`, message);

      // If 401 and we had a token, it's expired — clear & reload
      if (status === 401 && tokenManager.hasToken()) {
        tokenManager.clearToken();
        window.location.reload();
      }
    } else if (error.request) {
      console.error('[API] No response received:', error.message);
    } else {
      console.error('[API] Request setup error:', error.message);
    }
    return Promise.reject(error);
  },
);

export const STATIC_URL = import.meta.env.VITE_STATIC_URL || 'http://localhost:5027';

/**
 * Build a full URL for a static asset (image, uploaded file, etc.)
 * @param {string} path - e.g. "/uploads/abc.png"
 */
export const staticUrl = (path) => {
  if (!path) return '';
  if (path.startsWith('http')) return path;
  return `${STATIC_URL}${path}`;
};

export default apiClient;
