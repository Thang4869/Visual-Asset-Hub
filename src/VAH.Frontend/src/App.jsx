import React, { useEffect } from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { useAuth } from './hooks/useAuth';
import LoginPage from './components/LoginPage';
import { AppProvider, useAppContext } from './context/AppContext';
import AppHeader from './components/AppHeader';
import AppSidebar from './components/AppSidebar';
import DetailsPanel from './components/DetailsPanel';
import AssetDisplayer from './components/AssetDisplayer';
import UploadArea from './components/UploadArea';
import CollectionBrowser from './components/CollectionBrowser';
import ColorBoard from './components/ColorBoard';
import ShareDialog from './components/ShareDialog';
import TreeViewPanel from './components/TreeViewPanel';
import { ConfirmProvider } from './context/ConfirmContext';
import { useConfirm } from './context/ConfirmContext';
import * as assetsApiDirect from './api/assetsApi';
import './App.css';

/** Main authenticated layout — consumes state from AppContext */
function AppLayout() {
  const { confirm, prompt: showPrompt, alert: showAlert } = useConfirm();
  const {
    isAuthenticated, user, logout,
    // View
    viewMode, layoutMode, setLayoutMode,
    searchTerm, debouncedSearch, handleSearchChange,
    // Collections
    collections, selectedCollection, collectionItems, loading,
    breadcrumbPath, folderPath,
    breadcrumbClick, folderBreadcrumbClick, folderBreadcrumbRoot,
    navigateToCollection,
    handleCreateCollection, handleDeleteCollection, refreshItems,
    // Assets
    selectedAssetId, setSelectedAssetId, selectedAsset, selectedAssetIds,
    toggleSelectAsset, selectAllAssets, clearSelection,
    handleUpload, handleCreateFolder, handleCreateLink,
    handleCreateColorGroup, handleCreateColor,
    handleMoveAsset, handleMoveSelected, handleReorderAssets, handleMoveColorsToGroup,
    handleBulkDelete, handleBulkMove,
    // Tags
    handleAddTag,
    // Smart
    smartCollections, activeSmartCollection, smartItems,
    handleSelectSmartCollection, clearSmartCollection,
    // Share
    showShareDialog, setShowShareDialog,
    // Clipboard & Pin
    clipboard, pinnedItems, selectedFolderIds, setSelectedFolderIds, treeViewCollapsed,
    // Cross-concern
    handleSelectCollection, handleOpenFolder,
    handleSelectFolderItem, handleDeleteFolder, handleDeleteAsset,
    handleRenameAsset, handleRenameCollection,
    handleCopy, handleCut, handlePaste, handlePinItem, handleToggleTreeView,
    handleViewDetail, handleUngroupColor,
    handleNavigateToPinned,
  } = useAppContext();

  // ---- Auth gate (MUST be after all hooks) ----
  if (!isAuthenticated) return <Navigate to="/login" replace />;

  // ------- derived flags -------
  const isColorCollection = selectedCollection?.type === 'color';
  const showFolderActions = selectedCollection && (selectedCollection.type === 'image' || selectedCollection.type === 'default' || selectedCollection.type === 'link' || selectedCollection.type === 'color');
  const showLinkAction = selectedCollection && (selectedCollection.type === 'link' || selectedCollection.type === 'default');

  // Wrapper: prompt for collection name then delegate
  const handleAddCollectionWithPrompt = async () => {
    const name = await showPrompt({ message: 'Tên collection:', placeholder: 'Nhập tên...' });
    if (name) handleCreateCollection(name);
  };

  // ── Global clipboard paste: allow pasting images from external sources ──
  useEffect(() => {
    const handlePaste = (e) => {
      // Skip if user is typing in an input/textarea
      const tag = document.activeElement?.tagName;
      if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT') return;
      if (!selectedCollection) return;

      const items = e.clipboardData?.items;
      if (!items) return;

      const files = [];
      for (const item of items) {
        if (item.kind === 'file' && item.type.startsWith('image/')) {
          const file = item.getAsFile();
          if (file) {
            // Give a meaningful name since clipboard images come as "image.png"
            const ts = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
            const ext = file.type.split('/')[1] || 'png';
            const named = new File([file], `paste-${ts}.${ext}`, { type: file.type });
            files.push(named);
          }
        }
      }

      if (files.length > 0) {
        e.preventDefault();
        handleUpload(files);
      }
    };

    document.addEventListener('paste', handlePaste);
    return () => document.removeEventListener('paste', handlePaste);
  }, [selectedCollection, handleUpload]);

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
          onAddCollection={handleAddCollectionWithPrompt}
          pinnedItems={pinnedItems}
          onPinItem={handlePinItem}
          onNavigateToPinned={handleNavigateToPinned}
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
                  {breadcrumbPath.map((collection, idx) => (
                    <React.Fragment key={collection.id}>
                      <span className="breadcrumb-separator">›</span>
                      <button 
                        className={`breadcrumb-item ${idx === breadcrumbPath.length - 1 && folderPath.length === 0 ? 'current' : ''}`}
                        onClick={() => {
                          if (folderPath.length > 0 && idx === breadcrumbPath.length - 1) {
                            // Last breadcrumb when inside a folder — go back to collection root
                            folderBreadcrumbRoot();
                          } else {
                            breadcrumbClick(collection);
                          }
                        }}
                      >
                        {collection.name}
                      </button>
                    </React.Fragment>
                  ))}
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
              {(selectedAssetIds.size > 0 || selectedFolderIds.size > 0) && (
                <div className="bulk-actions-bar">
                  <span className="bulk-count">
                    {selectedAssetIds.size + selectedFolderIds.size} item đã chọn
                    {selectedFolderIds.size > 0 && ` (${selectedFolderIds.size} thư mục)`}
                  </span>
                  <button className="btn-secondary" onClick={selectAllAssets}>Chọn tất cả</button>
                  <button className="btn-secondary" onClick={() => { clearSelection(); setSelectedFolderIds && setSelectedFolderIds(new Set()); }}>Bỏ chọn</button>
                  <button className="btn-danger" onClick={async () => {
                    const ok = await confirm({ message: `Xóa ${selectedAssetIds.size + selectedFolderIds.size} item?`, confirmLabel: 'Xóa', variant: 'danger' });
                    if (!ok) return;
                    // Delete selected folders first
                    if (selectedFolderIds.size > 0) {
                      for (const fId of selectedFolderIds) {
                        try { await assetsApiDirect.deleteAsset(fId); } catch(e) { console.error(e); }
                      }
                      setSelectedFolderIds(new Set());
                    }
                    // Delete selected assets
                    if (selectedAssetIds.size > 0) {
                      handleBulkDelete();
                    } else {
                      // Only folders were deleted, refresh
                      refreshItems && refreshItems();
                    }
                  }}>
                    <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/></svg>
                    Xóa
                  </button>
                  <button className="btn-secondary" onClick={async () => {
                    const target = await showPrompt({ message: 'Nhập ID collection đích:', placeholder: 'ID...' });
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
                    onMoveAsset={handleMoveAsset}
                    selectedAssetIds={selectedAssetIds}
                    onCreateFolder={handleCreateFolder}
                    onOpenFolder={handleOpenFolder}
                    clipboard={clipboard}
                    onCopy={handleCopy}
                    onCut={handleCut}
                    onPaste={handlePaste}
                    onPinItem={handlePinItem}
                    onRenameAsset={handleRenameAsset}
                    onDeleteAsset={handleDeleteAsset}
                    onDeleteFolder={handleDeleteFolder}
                    refreshItems={refreshItems}
                    onViewDetail={handleViewDetail}
                    onUngroupColor={handleUngroupColor}
                    pinnedItems={pinnedItems}
                    showPrompt={showPrompt}
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
                    selectedFolderIds={selectedFolderIds}
                    onSelectFolderItem={handleSelectFolderItem}
                    onDeleteFolder={handleDeleteFolder}
                    onDeleteAsset={handleDeleteAsset}
                    onRenameAsset={handleRenameAsset}
                    onRenameCollection={handleRenameCollection}
                    onPinItem={handlePinItem}
                    clipboard={clipboard}
                    onCut={handleCut}
                    onCopy={handleCopy}
                    onPaste={handlePaste}
                    onViewDetail={handleViewDetail}
                    onCreateFolder={handleCreateFolder}
                    onCreateLink={handleCreateLink}
                    pinnedItems={pinnedItems}
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

        {/* ====== RIGHT TREE VIEW PANEL ====== */}
        {selectedCollection && !selectedAsset && (
          <TreeViewPanel
            collection={selectedCollection}
            subCollections={collectionItems.subCollections}
            items={collectionItems.items}
            selectedAssetId={selectedAssetId}
            onSelectAsset={(id) => toggleSelectAsset(id)}
            onSelectFolder={handleOpenFolder}
            onSelectCollection={navigateToCollection}
            collapsed={treeViewCollapsed}
            onToggleCollapsed={handleToggleTreeView}
            clipboard={clipboard}
            onCopy={handleCopy}
            onCut={handleCut}
            onPaste={handlePaste}
            onPinItem={handlePinItem}
            onRenameAsset={handleRenameAsset}
            onRenameCollection={handleRenameCollection}
            onDeleteFolder={handleDeleteFolder}
            onDeleteAsset={handleDeleteAsset}
          />
        )}

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

/** Root App component — defines routes, wraps layout with AppProvider */
function App() {
  const { isAuthenticated } = useAuth();

  return (
    <ConfirmProvider>
      <Routes>
        <Route
          path="/login"
          element={isAuthenticated ? <Navigate to="/" replace /> : <LoginPage />}
        />
        <Route path="/" element={<AppProvider><AppLayout /></AppProvider>} />
        <Route path="/collections/:collectionId" element={<AppProvider><AppLayout /></AppProvider>} />
        <Route path="/collections/:collectionId/folder/:folderId" element={<AppProvider><AppLayout /></AppProvider>} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </ConfirmProvider>
  );
}

export default App;