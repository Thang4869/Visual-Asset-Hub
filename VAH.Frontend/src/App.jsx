import React, { useState, useEffect, useRef, useMemo, useCallback } from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { useAuth } from './hooks/useAuth';
import LoginPage from './components/LoginPage';
import useCollections from './hooks/useCollections';
import useAssets from './hooks/useAssets';
import useTags from './hooks/useTags';
import useSignalR from './hooks/useSignalR';
import useUndoRedo from './hooks/useUndoRedo';
import useSmartCollections from './hooks/useSmartCollections';
import AppHeader from './components/AppHeader';
import AppSidebar from './components/AppSidebar';
import DetailsPanel from './components/DetailsPanel';
import AssetDisplayer from './components/AssetDisplayer';
import UploadArea from './components/UploadArea';
import CollectionBrowser from './components/CollectionBrowser';
import ColorBoard from './components/ColorBoard';
import ShareDialog from './components/ShareDialog';
import './App.css';

/** Main authenticated layout — renders for /, /collections/:id, /collections/:id/folder/:folderId */
function AppLayout() {
  const { isAuthenticated, user, logout } = useAuth();
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
    selectedAssetIds,
    toggleSelectAsset,
    selectAllAssets,
    clearSelection,
    handleUpload,
    handleCreateFolder,
    handleCreateLink,
    handleCreateColorGroup,
    handleCreateColor,
    handleMoveAsset,
    handleMoveSelected,
    handleReorderAssets,
    handleMoveColorsToGroup,
    handleBulkDelete,
    handleBulkMove,
    handleBulkTag,
    handleDeleteAsset,
  } = useAssets({ selectedCollection, currentFolderId, collectionItems, refreshItems });

  // ------- tags -------
  const { tags, createTag, deleteTag, getAssetTags, setAssetTags } = useTags();

  // ------- smart collections -------
  const {
    smartCollections,
    activeSmartCollection,
    smartItems,
    handleSelectSmartCollection,
    clearSmartCollection,
  } = useSmartCollections(isAuthenticated);

  // ------- smart collection item fetch -------
  // (handled inside useSmartCollections hook)

  // ------- real-time sync -------
  const signalRHandlers = useMemo(() => ({
    AssetsUploaded: () => refreshItems(),
    AssetCreated: () => refreshItems(),
    AssetDeleted: () => refreshItems(),
    AssetsBulkDeleted: () => refreshItems(),
    AssetsBulkMoved: () => refreshItems(),
    CollectionCreated: () => refreshItems(),
    CollectionUpdated: () => refreshItems(),
    CollectionDeleted: () => refreshItems(),
    TagsChanged: () => refreshItems(),
  }), [refreshItems]);

  useSignalR(signalRHandlers, isAuthenticated);

  // ------- undo/redo -------
  const { execute: executeCmd, undo, redo, canUndo, canRedo } = useUndoRedo();

  // ------- share dialog -------
  const [showShareDialog, setShowShareDialog] = useState(false);

  // Keyboard shortcuts: Ctrl+Z / Ctrl+Shift+Z
  useEffect(() => {
    const handleKeyDown = (e) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 'z') {
        e.preventDefault();
        if (e.shiftKey) { redo(); } else { undo(); }
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [undo, redo]);

  // ---- Auth gate (MUST be after all hooks) ----
  if (!isAuthenticated) return <Navigate to="/login" replace />;

  // ------- derived flags -------
  const isColorCollection = selectedCollection?.type === 'color';
  const showFolderActions = selectedCollection && (selectedCollection.type === 'image' || selectedCollection.type === 'default');
  const showLinkAction = selectedCollection && (selectedCollection.type === 'link' || selectedCollection.type === 'default');

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

  const handleAddTag = async (assetId) => {
    const tagName = prompt('Nhập tên tag:');
    if (!tagName) return;
    try {
      const tag = await createTag(tagName);
      await setAssetTags(assetId, [tag.id]);
      refreshItems();
    } catch (e) {
      console.error('Tag error:', e);
    }
  };

  return (
    <div className="app">
      {/* ====== TOP HEADER ====== */}
      <AppHeader
        user={user}
        logout={logout}
        searchTerm={searchTerm}
        onSearchChange={handleSearchChange}
      />

      {/* ====== APP BODY (Sidebar + Main + Details) ====== */}
      <div className="app-body">
        {/* ====== LEFT SIDEBAR ====== */}
        <AppSidebar
          collections={collections}
          selectedCollection={selectedCollection}
          smartCollections={smartCollections}
          activeSmartCollection={activeSmartCollection}
          onSelectCollection={handleSelectCollection}
          onCreateCollection={handleCreateCollection}
          onDeleteCollection={handleDeleteCollection}
          onSelectSmartCollection={handleSelectSmartCollection}
        />

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
                        <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"><path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71"/><path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71"/></svg>
                        Liên kết mới
                      </button>
                    )}
                    {isColorCollection && (
                      <button className="btn-secondary" onClick={handleCreateColorGroup}>Nhóm màu mới</button>
                    )}
                    <button className="btn-secondary" onClick={handleMoveSelected}>
                      <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><circle cx="18" cy="5" r="3"/><circle cx="6" cy="12" r="3"/><circle cx="18" cy="19" r="3"/><line x1="8.59" y1="13.51" x2="15.42" y2="17.49"/><line x1="15.41" y1="6.51" x2="8.59" y2="10.49"/></svg>
                      Di chuyển
                    </button>
                    {selectedCollection?.userId && (
                      <button className="btn-secondary" onClick={() => setShowShareDialog(true)}>
                        <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M4 12v8a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-8"/><polyline points="16 6 12 2 8 6"/><line x1="12" y1="2" x2="12" y2="15"/></svg>
                        Chia sẻ
                      </button>
                    )}
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

              {/* Bulk Actions Bar (when multi-select) */}
              {selectedAssetIds.size > 0 && (
                <div className="bulk-actions-bar">
                  <span className="bulk-count">{selectedAssetIds.size} item đã chọn</span>
                  <button className="btn-secondary" onClick={selectAllAssets}>Chọn tất cả</button>
                  <button className="btn-secondary" onClick={clearSelection}>Bỏ chọn</button>
                  <button className="btn-danger" onClick={handleBulkDelete}>
                    <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/></svg>
                    Xóa
                  </button>
                  <button className="btn-secondary" onClick={() => {
                    const target = prompt('Nhập ID collection đích:');
                    if (target) handleBulkMove(parseInt(target), null);
                  }}>
                    <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/></svg>
                    Di chuyển
                  </button>
                </div>
              )}

              {/* Content Area */}
              <div className="main-content">
                {isColorCollection ? (
                  <ColorBoard
                    items={collectionItems.items}
                    onCreateColor={handleCreateColor}
                    onCreateGroup={handleCreateColorGroup}
                    onSelectAsset={toggleSelectAsset}
                    onMoveColorsToGroup={handleMoveColorsToGroup}
                    selectedAssetIds={selectedAssetIds}
                  />
                ) : viewMode === 'browser' ? (
                  <CollectionBrowser
                    assets={collectionItems.items}
                    subCollections={collectionItems.subCollections}
                    onSelectCollection={navigateToCollection}
                    onSelectFolder={handleOpenFolder}
                    onMoveAsset={handleMoveAsset}
                    onSelectAsset={toggleSelectAsset}
                    selectedAssetId={selectedAssetId}
                    selectedAssetIds={selectedAssetIds}
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
          ) : activeSmartCollection ? (
            <div className="smart-collection-view">
              <div className="main-toolbar">
                <div className="toolbar-breadcrumbs">
                  <button className="breadcrumb-item" onClick={clearSmartCollection}>
                    <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" style={{marginRight: 4, verticalAlign: 'middle'}}><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/></svg>
                    Trang chủ
                  </button>
                  <span className="breadcrumb-separator">›</span>
                  <span className="breadcrumb-item current">{activeSmartCollection.icon} {activeSmartCollection.name}</span>
                </div>
              </div>
              <div className="main-content">
                <CollectionBrowser
                  assets={smartItems}
                  subCollections={[]}
                  onSelectCollection={() => {}}
                  onSelectFolder={() => {}}
                  onMoveAsset={() => {}}
                  onSelectAsset={toggleSelectAsset}
                  selectedAssetId={selectedAssetId}
                  selectedAssetIds={selectedAssetIds}
                  loading={false}
                  searchTerm={debouncedSearch}
                  layoutMode={layoutMode}
                  onReorder={() => {}}
                />
              </div>
            </div>
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
          <DetailsPanel
            asset={selectedAsset}
            collectionName={selectedCollection?.name}
            onClose={() => setSelectedAssetId(null)}
            onAddTag={handleAddTag}
          />
        )}
      </div>

      {/* Share Dialog */}
      {showShareDialog && selectedCollection && (
        <ShareDialog
          collectionId={selectedCollection.id}
          collectionName={selectedCollection.name}
          onClose={() => setShowShareDialog(false)}
        />
      )}
    </div>
  );
}

/** Root App component — defines routes */
function App() {
  const { isAuthenticated } = useAuth();

  return (
    <Routes>
      <Route
        path="/login"
        element={isAuthenticated ? <Navigate to="/" replace /> : <LoginPage />}
      />
      <Route path="/" element={<AppLayout />} />
      <Route path="/collections/:collectionId" element={<AppLayout />} />
      <Route path="/collections/:collectionId/folder/:folderId" element={<AppLayout />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

export default App;