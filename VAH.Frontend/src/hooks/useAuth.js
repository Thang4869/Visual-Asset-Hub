import React, { createContext, useContext, useState, useCallback, useEffect } from 'react';
import * as authApi from '../api/authApi';
import { setToken, getToken, clearToken } from '../api/client';

const AuthContext = createContext(null);

/**
 * Provides auth state & actions to the component tree.
 *
 * Persisted values in localStorage:
 *   - vah_token   (JWT)
 *   - vah_user    (JSON: { userId, email, displayName })
 */
export function AuthProvider({ children }) {
  const [user, setUser] = useState(() => {
    try {
      const stored = localStorage.getItem('vah_user');
      return stored ? JSON.parse(stored) : null;
    } catch {
      return null;
    }
  });
  const [isAuthenticated, setIsAuthenticated] = useState(() => !!getToken());
  const [authLoading, setAuthLoading] = useState(false);
  const [authError, setAuthError] = useState(null);

  // Keep isAuthenticated in sync if token is cleared externally
  useEffect(() => {
    setIsAuthenticated(!!getToken());
  }, [user]);

  const persistUser = (data) => {
    const userInfo = { userId: data.userId, email: data.email, displayName: data.displayName };
    localStorage.setItem('vah_user', JSON.stringify(userInfo));
    setToken(data.token);
    setUser(userInfo);
    setIsAuthenticated(true);
  };

  const login = useCallback(async (email, password) => {
    setAuthLoading(true);
    setAuthError(null);
    try {
      const data = await authApi.login(email, password);
      persistUser(data);
      return data;
    } catch (err) {
      const msg =
        err.response?.data?.detail ||
        err.response?.data?.title ||
        err.response?.data ||
        'Đăng nhập thất bại';
      setAuthError(typeof msg === 'string' ? msg : JSON.stringify(msg));
      throw err;
    } finally {
      setAuthLoading(false);
    }
  }, []);

  const register = useCallback(async (displayName, email, password) => {
    setAuthLoading(true);
    setAuthError(null);
    try {
      const data = await authApi.register(displayName, email, password);
      persistUser(data);
      return data;
    } catch (err) {
      const msg =
        err.response?.data?.detail ||
        err.response?.data?.title ||
        err.response?.data ||
        'Đăng ký thất bại';
      setAuthError(typeof msg === 'string' ? msg : JSON.stringify(msg));
      throw err;
    } finally {
      setAuthLoading(false);
    }
  }, []);

  const logout = useCallback(() => {
    clearToken();
    localStorage.removeItem('vah_user');
    setUser(null);
    setIsAuthenticated(false);
  }, []);

  return React.createElement(
    AuthContext.Provider,
    { value: { user, isAuthenticated, authLoading, authError, login, register, logout, setAuthError } },
    children,
  );
}

/** Convenience hook */
export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within <AuthProvider>');
  return ctx;
}
