import React, { useState } from 'react';
import { useAuth } from '../hooks/useAuth';
import './LoginPage.css';

export default function LoginPage() {
  const { login, register, authLoading, authError, setAuthError } = useAuth();
  const [mode, setMode] = useState('login'); // 'login' | 'register'
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [displayName, setDisplayName] = useState('');

  const switchMode = () => {
    setMode((m) => (m === 'login' ? 'register' : 'login'));
    setAuthError(null);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      if (mode === 'login') {
        await login(email, password);
      } else {
        await register(displayName, email, password);
      }
    } catch {
      // error already stored in authError
    }
  };

  return (
    <div className="login-page">
      <div className="login-card">
        <div className="login-logo">
          <div className="login-logo-icon">
            <svg viewBox="0 0 24 24" width="28" height="28" fill="none" stroke="#fff" strokeWidth="2">
              <rect x="3" y="3" width="7" height="7" rx="1" />
              <rect x="14" y="3" width="7" height="7" rx="1" />
              <rect x="3" y="14" width="7" height="7" rx="1" />
              <rect x="14" y="14" width="7" height="7" rx="1" />
            </svg>
          </div>
          <span className="login-logo-text">Visual Asset Hub</span>
        </div>

        <h2 className="login-title">
          {mode === 'login' ? 'Đăng nhập' : 'Tạo tài khoản'}
        </h2>

        {authError && <div className="login-error">{authError}</div>}

        <form onSubmit={handleSubmit} className="login-form">
          {mode === 'register' && (
            <label className="login-field">
              <span>Tên hiển thị</span>
              <input
                type="text"
                value={displayName}
                onChange={(e) => setDisplayName(e.target.value)}
                placeholder="Nhập tên hiển thị"
                required
                autoFocus
              />
            </label>
          )}

          <label className="login-field">
            <span>Email</span>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="you@example.com"
              required
              autoFocus={mode === 'login'}
            />
          </label>

          <label className="login-field">
            <span>Mật khẩu</span>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
              required
              minLength={6}
            />
          </label>

          <button type="submit" className="login-submit" disabled={authLoading}>
            {authLoading
              ? 'Đang xử lý...'
              : mode === 'login'
                ? 'Đăng nhập'
                : 'Đăng ký'}
          </button>
        </form>

        <p className="login-switch">
          {mode === 'login' ? 'Chưa có tài khoản?' : 'Đã có tài khoản?'}{' '}
          <button type="button" onClick={switchMode} className="login-switch-btn">
            {mode === 'login' ? 'Đăng ký' : 'Đăng nhập'}
          </button>
        </p>
      </div>
    </div>
  );
}
