import React, { useState, useEffect, useRef } from 'react';
import useCollections from './hooks/useCollections';
import useAssets from './hooks/useAssets';
import { staticUrl } from './api/client';
import CollectionTree from './components/CollectionTree';
import AssetDisplayer from './components/AssetDisplayer';
import UploadArea from './components/UploadArea';
import CollectionBrowser from './components/CollectionBrowser';
import ColorBoard from './components/ColorBoard';
import './App.css';

function App() {
  const [viewMode, setViewMode] = useState('browser');
  const [layoutMode, setLayoutMode] = useState('grid');
  const [searchTerm, setSearchTerm] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const debounceRef = useRef(null);

  // Debounce search input (300ms)
  const handleSearchChange = (value) => {
    setSearchTerm(value);
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => setDebouncedSearch(value), 300);
  };

  useEffect(() => {
    return () => { if (debounceRef.current) clearTimeout(debounceRef.current); };
  }, []);

  // ------- collection state & actions -------
  const {
    collections,
    selectedCollection,
    collectionItems,
    loading,
    breadcrumbPath,
    folderPath,
    currentFolderId,
    selectCollection,
    breadcrumbClick,
    navigateToCollection,
    openFolder,
    folderBreadcrumbClick,
    folderBreadcrumbRoot,
    handleCreateCollection,
    handleDeleteCollection,
    refreshItems,
  } = useCollections();

  // ------- asset state & actions -------
  const {
    selectedAssetId,
    setSelectedAssetId,
    selectedAsset,
    handleUpload,
    handleCreateFolder,
    handleCreateLink,
    handleCreateColorGroup,
    handleCreateColor,
    handleMoveAsset,
    handleMoveSelected,
    handleReorderAssets,
  } = useAssets({ selectedCollection, currentFolderId, collectionItems, refreshItems });

  // ------- derived flags -------
  const isColorCollection = selectedCollection?.type === 'color';
  const showFolderActions = selectedCollection && (selectedCollection.type === 'image' || selectedCollection.type === 'default');
  const showLinkAction = selectedCollection && (selectedCollection.type === 'link' || selectedCollection.type === 'default');

  const formatDate = (dateStr) => {
    if (!dateStr) return '—';
    const d = new Date(dateStr);
    return d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
  };

  const getContentTypeLabel = (type) => {
    const map = { image: 'Hình ảnh', link: 'Liên kết', color: 'Màu sắc', folder: 'Thư mục', file: 'Tệp tin', default: 'Tệp tin' };
    return map[type] || type;
  };

  const handleSelectCollection = (collection, path = []) => {
    selectCollection(collection, path);
    setSelectedAssetId(null);
    setSearchTerm('');
    setDebouncedSearch('');
  };

  const handleOpenFolder = (folder) => {
    openFolder(folder);
    setSelectedAssetId(null);
  };

  return (
    <div className="app">
      {/* ====== TOP HEADER ====== */}
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
          <div className="search-wrapper">
            <span className="search-icon-el">
              <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"><circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/></svg>
            </span>
            <input
              type="text"
              id="global-search"
              name="search"
              placeholder="Tìm kiếm..."
              value={searchTerm}
              onChange={(e) => handleSearchChange(e.target.value)}
              className="search-input"
              autoComplete="off"
            />
          </div>
        </div>

        <div className="header-right">
          <button className="header-icon-btn" title="Thư mục">
            <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/></svg>
          </button>
          <button className="header-icon-btn" title="Cài đặt">
            <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"/></svg>
          </button>
          <button className="header-icon-btn" title="Thông báo">
            <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"/><path d="M13.73 21a2 2 0 0 1-3.46 0"/></svg>
            <span className="notif-dot"></span>
          </button>
          <div className="user-avatar" title="Người dùng">
            VP
          </div>
        </div>
      </header>

      {/* ====== APP BODY (Sidebar + Main + Details) ====== */}
      <div className="app-body">
        {/* ====== LEFT SIDEBAR ====== */}
        <aside className="app-sidebar">
          <div className="sidebar-header">
            <h3>Tài liệu của tôi</h3>
            <button className="add-collection-btn" onClick={() => {
              const name = prompt('Tên collection:');
              if (name) handleCreateCollection(name);
            }}>+</button>
          </div>
          <div className="sidebar-scroll">
            <CollectionTree 
              collections={collections}
              selectedCollection={selectedCollection}
              onSelectCollection={(c) => handleSelectCollection(c, [c])}
              onCreateCollection={handleCreateCollection}
              onDeleteCollection={handleDeleteCollection}
            />
          </div>
        </aside>

        {/* ====== MAIN CONTENT AREA ====== */}
        <main className="app-main">
          {selectedCollection ? (
            <>
              {/* Toolbar: Breadcrumbs + Actions */}
              <div className="main-toolbar">
                <div className="toolbar-breadcrumbs">
                  <button 
                    className="breadcrumb-item"
                    onClick={() => handleSelectCollection(null, [])}
                  >
                    <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" style={{marginRight: 4, verticalAlign: 'middle'}}><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/></svg>
                    Trang chủ
                  </button>
                  {breadcrumbPath.map((collection) => (
                    <React.Fragment key={collection.id}>
                      <span className="breadcrumb-separator">›</span>
                      <button 
                        className="breadcrumb-item"
                        onClick={() => breadcrumbClick(collection)}
                      >
                        {collection.name}
                      </button>
                    </React.Fragment>
                  ))}
                  {folderPath.length > 0 && (
                    <>
                      <span className="breadcrumb-separator">›</span>
                      <button className="breadcrumb-item" onClick={folderBreadcrumbRoot}>
                        {selectedCollection.name}
                      </button>
                    </>
                  )}
                  {folderPath.map((folder, idx) => (
                    <React.Fragment key={folder.id}>
                      <span className="breadcrumb-separator">›</span>
                      <button
                        className={`breadcrumb-item ${idx === folderPath.length - 1 ? 'current' : ''}`}
                        onClick={() => folderBreadcrumbClick(folder)}
                      >
                        {folder.fileName}
                      </button>
                    </React.Fragment>
                  ))}
                </div>

                <div className="toolbar-actions">
                  <div className="toolbar-actions-left">
                    <button className="btn-primary" onClick={() => document.querySelector('.upload-area')?.click()}>
                      <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="17 8 12 3 7 8"/><line x1="12" y1="3" x2="12" y2="15"/></svg>
                      Tải lên
                    </button>
                    {showFolderActions && (
                      <button className="btn-secondary" onClick={handleCreateFolder}>
                        <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/><line x1="12" y1="11" x2="12" y2="17"/><line x1="9" y1="14" x2="15" y2="14"/></svg>
                        Thư mục mới
                      </button>
                    )}
                    {showLinkAction && (
                      <button className="btn-secondary" onClick={handleCreateLink}>
                        <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/></svg>
                        Tải xuống
                      </button>
                    )}
                    {isColorCollection && (
                      <button className="btn-secondary" onClick={handleCreateColorGroup}>Nhóm màu mới</button>
                    )}
                    <button className="btn-secondary" onClick={handleMoveSelected}>
                      <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><circle cx="18" cy="5" r="3"/><circle cx="6" cy="12" r="3"/><circle cx="18" cy="19" r="3"/><line x1="8.59" y1="13.51" x2="15.42" y2="17.49"/><line x1="15.41" y1="6.51" x2="8.59" y2="10.49"/></svg>
                      Chia sẻ
                    </button>
                  </div>
                  <div className="toolbar-actions-right">
                    <div className="view-switcher">
                      <button
                        className={layoutMode === 'list' ? 'active' : ''}
                        onClick={() => setLayoutMode('list')}
                        title="Danh sách"
                      >
                        <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"><line x1="8" y1="6" x2="21" y2="6"/><line x1="8" y1="12" x2="21" y2="12"/><line x1="8" y1="18" x2="21" y2="18"/><line x1="3" y1="6" x2="3.01" y2="6"/><line x1="3" y1="12" x2="3.01" y2="12"/><line x1="3" y1="18" x2="3.01" y2="18"/></svg>
                      </button>
                      <button
                        className={layoutMode === 'grid' ? 'active' : ''}
                        onClick={() => setLayoutMode('grid')}
                        title="Lưới"
                      >
                        <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><rect x="3" y="3" width="7" height="7"/><rect x="14" y="3" width="7" height="7"/><rect x="14" y="14" width="7" height="7"/><rect x="3" y="14" width="7" height="7"/></svg>
                      </button>
                      <button
                        className={layoutMode === 'masonry' ? 'active' : ''}
                        onClick={() => setLayoutMode('masonry')}
                        title="Masonry"
                      >
                        <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><rect x="3" y="3" width="7" height="9"/><rect x="14" y="3" width="7" height="5"/><rect x="14" y="12" width="7" height="9"/><rect x="3" y="16" width="7" height="5"/></svg>
                      </button>
                    </div>
                  </div>
                </div>
              </div>

              {/* Content Area */}
              <div className="main-content">
                {isColorCollection ? (
                  <ColorBoard
                    items={collectionItems.items}
                    onCreateColor={handleCreateColor}
                    onCreateGroup={handleCreateColorGroup}
                  />
                ) : viewMode === 'browser' ? (
                  <CollectionBrowser
                    assets={collectionItems.items}
                    subCollections={collectionItems.subCollections}
                    onSelectCollection={navigateToCollection}
                    onSelectFolder={handleOpenFolder}
                    onMoveAsset={handleMoveAsset}
                    onSelectAsset={setSelectedAssetId}
                    selectedAssetId={selectedAssetId}
                    loading={loading}
                    searchTerm={debouncedSearch}
                    layoutMode={layoutMode}
                    onReorder={handleReorderAssets}
                  />
                ) : (
                  <AssetDisplayer 
                    assets={collectionItems.items.filter(i => !i.isFolder && i.contentType === 'image')}
                    subCollections={collectionItems.subCollections}
                    viewMode="canvas"
                    onSelectCollection={navigateToCollection}
                    loading={loading}
                  />
                )}
              </div>

              {/* Upload section */}
              <div className="upload-section">
                <UploadArea onUpload={handleUpload} />
              </div>
            </>
          ) : (
            <div className="home-view">
              <div className="home-header">
                <h2>Chào mừng đến Visual Asset Hub</h2>
                <p>Chọn một bộ sưu tập hoặc tạo mới để bắt đầu</p>
              </div>
              {collections.length > 0 && (
                <div className="home-collections">
                  <h3>Bộ sưu tập của bạn</h3>
                  <div className="collections-quick-access">
                    {collections.map(collection => (
                      <div
                        key={collection.id}
                        className="collection-quick-card"
                        onClick={() => handleSelectCollection(collection, [collection])}
                      >
                        <div className="card-icon">📁</div>
                        <div className="card-name">{collection.name}</div>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          )}
        </main>

        {/* ====== RIGHT DETAILS PANEL ====== */}
        {selectedAsset && (
          <aside className="details-panel">
            <div className="details-panel-header">
              <h3>Tài liệu</h3>
              <button className="details-close-btn" onClick={() => setSelectedAssetId(null)} title="Đóng">
                <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
              </button>
            </div>

            {/* Large icon / thumbnail */}
            <div className="details-preview">
              {selectedAsset.contentType === 'image' ? (
                <img
                  src={staticUrl(selectedAsset.filePath)}
                  alt={selectedAsset.fileName}
                  onError={(e) => { e.target.style.display = 'none'; }}
                />
              ) : selectedAsset.contentType === 'color' ? (
                <div style={{ width: 120, height: 120, borderRadius: 12, backgroundColor: selectedAsset.filePath, boxShadow: '0 4px 16px rgba(0,0,0,0.3)' }} />
              ) : (
                <div className="details-preview-icon">
                  {selectedAsset.isFolder ? '📁' : selectedAsset.contentType === 'link' ? '🔗' : '📄'}
                </div>
              )}
            </div>

            {/* Info section */}
            <div className="details-info">
              <h3 className="details-info-title">{selectedAsset.fileName}</h3>
              
              <div className="details-info-section">
                <h4>Thông tin</h4>
                <div className="details-info-row">
                  <span className="details-info-label">Title</span>
                  <span className="details-info-value">{selectedAsset.fileName}</span>
                </div>
                <div className="details-info-row">
                  <span className="details-info-label">Đơn giữ/đặn</span>
                  <span className="details-info-value">{selectedCollection?.name || '—'}</span>
                </div>
                <div className="details-info-row">
                  <span className="details-info-label">Tải lên</span>
                  <span className="details-info-value">{formatDate(selectedAsset.createdAt)}</span>
                </div>
                <div className="details-info-row">
                  <span className="details-info-label">Loại tệp</span>
                  <span className="details-info-value">{getContentTypeLabel(selectedAsset.contentType)}</span>
                </div>
                <div className="details-info-row">
                  <span className="details-info-label">Thư mục gốc</span>
                  <span className="details-info-value" style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                    {selectedAsset.isFolder ? '—' : '■'}
                  </span>
                </div>
              </div>
            </div>

            {/* Preview section for images/videos */}
            {selectedAsset.contentType === 'image' && (
              <div className="details-preview-section">
                <h4>Chỉ xem</h4>
                <div className="details-full-preview">
                  <img
                    src={staticUrl(selectedAsset.filePath)}
                    alt={selectedAsset.fileName}
                    onError={(e) => { e.target.parentElement.style.display = 'none'; }}
                  />
                </div>
              </div>
            )}
          </aside>
        )}
      </div>
    </div>
  );
}

export default App;