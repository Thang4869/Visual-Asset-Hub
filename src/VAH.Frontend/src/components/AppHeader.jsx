import React, { useState, useEffect, useRef } from 'react';
import * as settingsApi from '../api/settingsApi';
import * as notificationsApi from '../api/notificationsApi';
import * as searchApi from '../api/searchApi';
import './AppHeader.css';

/**
 * Application header bar — logo, search, user actions.
 */
export default function AppHeader({ user, logout, searchTerm, onSearchChange }) {
  const [showSettings, setShowSettings] = useState(false);
  const [showNoti, setShowNoti] = useState(false);
  const [settings, setSettings] = useState({ theme: 'light', layoutType: 'grid', receiveEmailNotifications: true });
  const [notifications, setNotifications] = useState([]);
  const [searchResults, setSearchResults] = useState([]);
  const [isSearching, setIsSearching] = useState(false);
  const [showSearchDrop, setShowSearchDrop] = useState(false);
  
  const settingsRef = useRef(null);
  const notiRef = useRef(null);
  const searchRef = useRef(null);

  // Fetch init data
  useEffect(() => {
    if (user) {
      settingsApi.getSettings().then(res => setSettings(res)).catch(console.error);
      notificationsApi.getNotifications().then(res => setNotifications(res)).catch(console.error);
    }
  }, [user]);

  // Handle click outside to close dropdowns
  useEffect(() => {
    function handleClickOutside(event) {
      if (settingsRef.current && !settingsRef.current.contains(event.target)) {
        setShowSettings(false);
      }
      if (notiRef.current && !notiRef.current.contains(event.target)) {
        setShowNoti(false);
      }
      if (searchRef.current && !searchRef.current.contains(event.target)) {
        setShowSearchDrop(false);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  // Handle global search debounce
  useEffect(() => {
    if (!searchTerm || searchTerm.trim().length < 2) {
      setSearchResults([]);
      setShowSearchDrop(false);
      return;
    }

    const delayDebounceFn = setTimeout(async () => {
      setIsSearching(true);
      setShowSearchDrop(true);
      try {
        const res = await searchApi.globalSearch(searchTerm);
        setSearchResults(res);
      } catch (err) {
        console.error('Search error', err);
        setSearchResults([]);
      } finally {
        setIsSearching(false);
      }
    }, 500);

    return () => clearTimeout(delayDebounceFn);
  }, [searchTerm]);

  const handleUpdateSetting = async (key, value) => {
    const updated = { ...settings, [key]: value };
    setSettings(updated);
    try {
      await settingsApi.updateSettings({ [key]: value });
    } catch(err) {
      console.error(err);
    }
  };

  const handleMarkAsRead = async (id) => {
    try {
      await notificationsApi.markAsRead(id);
      setNotifications(prev => prev.map(n => n.id === id ? { ...n, isRead: true } : n));
    } catch(err) {
      console.error(err);
    }
  };

  const toggleSettings = () => {
    setShowSettings(!showSettings);
    setShowNoti(false);
  };

  const toggleNoti = () => {
    setShowNoti(!showNoti);
    setShowSettings(false);
  };

  const hasUnread = notifications.some(n => !n.isRead);

  return (
    <header className="app-header">
      <div className="header-left">
        <button className="hamburger-btn" title="Menu">
          <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"><line x1="3" y1="6" x2="21" y2="6"/><line x1="3" y1="12" x2="21" y2="12"/><line x1="3" y1="18" x2="21" y2="18"/></svg>
        </button>
        <div className="logo">
          <div className="logo-icon">
            <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="white" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round"><circle cx="12" cy="12" r="10"/><path d="M12 8v8M8 12h8"/></svg>
          </div>
          <span className="logo-text">Visual Asset Hub</span>
        </div>
      </div>

      <div className="header-center">
        <div className="search-wrapper" ref={searchRef}>
          <span className="search-icon-el">
            <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"><circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/></svg>
          </span>
          <input
            type="text"
            id="global-search"
            name="search"
            placeholder="Tìm kiếm..."
            value={searchTerm}
            onChange={(e) => onSearchChange(e.target.value)}
            onFocus={() => { if(searchTerm && searchTerm.trim().length >= 2) setShowSearchDrop(true); }}
            className="search-input"
            autoComplete="off"
          />
          
          {showSearchDrop && (
            <div className="search-dropdown-menu">
              {isSearching ? (
                <div className="search-loading">Đang tìm kiếm...</div>
              ) : searchResults.length === 0 ? (
                <div className="search-no-result">Không tìm thấy kết quả phù hợp.</div>
              ) : (
                <div className="search-result-list">
                  {searchResults.map((item, idx) => (
                    <div key={idx} className="search-result-item">
                      <div className="search-result-icon">
                        {item.type === 'Asset' ? (
                          <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><rect x="3" y="3" width="18" height="18" rx="2" ry="2"/><circle cx="8.5" cy="8.5" r="1.5"/><polyline points="21 15 16 10 5 21"/></svg>
                        ) : (
                          <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/></svg>
                        )}
                      </div>
                      <div className="search-result-info">
                        <div className="search-result-name">{item.name}</div>
                        <div className="search-result-type">{item.type}</div>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}
        </div>
      </div>

      <div className="header-right">
        <button className="header-icon-btn" title="Thư mục">
          <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/></svg>
        </button>

        <div style={{ position: 'relative', display: 'flex', alignItems: 'center' }} ref={settingsRef}>
          <button className={`header-icon-btn ${showSettings ? 'active' : ''}`} title="Cài đặt" onClick={toggleSettings}>
            <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"/></svg>
          </button>
          
          {showSettings && (
            <div className="header-dropdown">
              <div className="header-dropdown-header">
                <span>Cài đặt hệ thống</span>
              </div>
              <div className="header-dropdown-content settings-wrapper">
                
                {/* APPEARANCE SECTION */}
                <div className="settings-section">
                  <div className="settings-section-title">Giao diện & Tiện ích</div>
                  
                  <div className="settings-row">
                    <div className="settings-info">
                      <div className="settings-label">Chế độ nền (Theme)</div>
                      <div className="settings-desc">Tùy chỉnh sáng tối theo ý thích</div>
                    </div>
                    <div className="settings-action">
                      <select 
                        className="dropdown-select small" 
                        value={settings.theme || 'dark'} 
                        onChange={e => handleUpdateSetting('theme', e.target.value)}
                      >
                        <option value="light">Sáng (Light)</option>
                        <option value="dark">Tối (Navy)</option>
                        <option value="system">Hệ thống</option>
                      </select>
                    </div>
                  </div>

                  <div className="settings-row">
                    <div className="settings-info">
                      <div className="settings-label">Ngôn ngữ hiển thị</div>
                      <div className="settings-desc">Chọn ngôn ngữ cho nền tảng</div>
                    </div>
                    <div className="settings-action">
                      <select 
                        className="dropdown-select small" 
                        value={settings.language || 'vi'} 
                        onChange={e => handleUpdateSetting('language', e.target.value)}
                      >
                        <option value="vi">Tiếng Việt</option>
                        <option value="en">English</option>
                      </select>
                    </div>
                  </div>
                  
                  <div className="settings-row">
                    <div className="settings-info">
                      <div className="settings-label">Mật độ hiển thị</div>
                      <div className="settings-desc">Khoảng cách giữa các thành phần</div>
                    </div>
                    <div className="settings-action">
                      <select 
                        className="dropdown-select small" 
                        value={settings.density || 'comfortable'} 
                        onChange={e => handleUpdateSetting('density', e.target.value)}
                      >
                        <option value="compact">Thu gọn</option>
                        <option value="comfortable">Thoải mái</option>
                      </select>
                    </div>
                  </div>
                </div>

                <div className="settings-divider"></div>

                {/* NOTIFICATIONS SECTION */}
                <div className="settings-section">
                  <div className="settings-section-title">Thông báo & Âm thanh</div>
                  
                  <div className="settings-row">
                    <div className="settings-info">
                      <div className="settings-label">Cập nhật qua Email</div>
                      <div className="settings-desc">Nhận báo cáo sự kiện qua thư điện tử</div>
                    </div>
                    <div className="settings-action">
                      <label className="toggle-switch" title="Chuyển đổi nhận email">
                        <input 
                          type="checkbox" 
                          checked={settings.receiveEmailNotifications ?? true} 
                          onChange={e => handleUpdateSetting('receiveEmailNotifications', e.target.checked)} 
                        />
                        <span className="slider"></span>
                      </label>
                    </div>
                  </div>

                  <div className="settings-row">
                    <div className="settings-info">
                      <div className="settings-label">Âm thanh thông báo</div>
                      <div className="settings-desc">Phát tiếng 'ping' ngắn khi có tin mới</div>
                    </div>
                    <div className="settings-action">
                      <label className="toggle-switch" title="Bật âm thanh">
                        <input 
                          type="checkbox" 
                          checked={settings.soundEnabled || false} 
                          onChange={e => handleUpdateSetting('soundEnabled', e.target.checked)} 
                        />
                        <span className="slider"></span>
                      </label>
                    </div>
                  </div>
                  
                  <div className="settings-row">
                    <div className="settings-info">
                      <div className="settings-label">Hiển thị popup (Toast)</div>
                      <div className="settings-desc">Hiện thông báo góc màn hình</div>
                    </div>
                    <div className="settings-action">
                      <label className="toggle-switch" title="Bật popup">
                        <input 
                          type="checkbox" 
                          checked={settings.toastEnabled ?? true} 
                          onChange={e => handleUpdateSetting('toastEnabled', e.target.checked)} 
                        />
                        <span className="slider"></span>
                      </label>
                    </div>
                  </div>
                </div>

                <div className="settings-divider"></div>

                <div className="settings-section" style={{ paddingBottom: '20px' }}>
                  <div className="settings-footer-link" onClick={() => setShowSettings(false)}>
                    Xem toàn bộ cài đặt nâng cao &rarr;
                  </div>
                </div>

              </div>
            </div>
          )}
        </div>

        <div style={{ position: 'relative', display: 'flex', alignItems: 'center' }} ref={notiRef}>
          <button className={`header-icon-btn ${showNoti ? 'active' : ''}`} title="Thông báo" onClick={toggleNoti}>
            <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"/><path d="M13.73 21a2 2 0 0 1-3.46 0"/></svg>
            {hasUnread && <span className="notif-dot" style={{ position: 'absolute', top: 4, right: 4, background: 'var(--notif-red)', width: 8, height: 8, borderRadius: '50%' }}></span>}
          </button>
          
          {showNoti && (
            <div className="header-dropdown">
              <div className="header-dropdown-header">
                <span>Thông báo</span>
                {hasUnread && (
                  <span style={{ fontSize: '0.75rem', color: 'var(--accent)', cursor: 'pointer' }} onClick={() => notifications.filter(n => !n.isRead).forEach(n => handleMarkAsRead(n.id))}>Đánh dấu đọc hết</span>
                )}
              </div>
              <div className="notif-list">
                {notifications.length === 0 ? (
                  <div className="notif-empty">
                    <svg viewBox="0 0 24 24" width="32" height="32" fill="none" stroke="currentColor" strokeWidth="1.5"><path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"/><path d="M13.73 21a2 2 0 0 1-3.46 0"/></svg>
                    <span>Bạn chưa có thông báo mới nào.</span>
                  </div>
                ) : (
                  notifications.map(n => (
                    <div key={n.id} className={`notif-item ${!n.isRead ? 'unread' : ''}`}>
                      <div className="notif-title">{n.title}</div>
                      <div className="notif-msg">{n.message}</div>
                      {!n.isRead && (
                        <div className="notif-action" onClick={() => handleMarkAsRead(n.id)}>
                          Đánh dấu đã đọc
                        </div>
                      )}
                    </div>
                  ))
                )}
              </div>
            </div>
          )}
        </div>

        <button className="header-icon-btn" title="Đăng xuất" onClick={logout}>
          <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" y1="12" x2="9" y2="12"/></svg>
        </button>
        <div className="user-avatar" title={user?.displayName || 'User'}>
          {user?.displayName ? user.displayName.slice(0, 2).toUpperCase() : 'U'}
        </div>
      </div>
    </header>
  );
}



