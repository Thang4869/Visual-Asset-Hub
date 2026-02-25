import axios from 'axios';

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5027/api',
  timeout: 30000,
  headers: {
    'Accept': 'application/json',
  },
});

// Response interceptor — normalise errors
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response) {
      // Server returned an error response (4xx / 5xx)
      const { status, data } = error.response;
      const message = data?.detail || data?.title || error.message;
      console.error(`[API ${status}]`, message);
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
