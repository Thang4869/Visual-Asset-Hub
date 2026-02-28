# Bản Đánh Giá OOP - Visual Asset Hub (VAH)

> **Ngày tạo:** 2026-02-27  
> **Cập nhật lần cuối:** 2026-02-28  
> **Mục đích:** Đánh giá mức độ áp dụng OOP trong toàn bộ dự án, làm cơ sở cho quá trình refactor.  
> **Trạng thái:** ✅ Phase 1, 2, 3 hoàn tất (15/23 tasks) — Phase 4, 5 chưa bắt đầu

---

## Tổng Quan Dự Án

| Layer | Ngôn ngữ | Framework |
|-------|----------|-----------|
| Backend | C# (.NET 9) | ASP.NET Core, EF Core, Identity, SignalR |
| Frontend | JavaScript (JSX) | React 19, Vite 7, Axios, react-router-dom v7 |

---

## I. BACKEND (C# / .NET)

### 1. Models — Refactored ✅

> **Phase 1 hoàn tất:** Tất cả models đã được refactor: TPH inheritance, enums, domain behavior, navigation properties.

| File | Trạng thái | Vấn đề |
|------|-----------|--------|
| `Models/Asset.cs` | � Refactored | **[Task #3]** `ContentType` đã chuyển sang enum `AssetContentType`. **[Task #6]** TPH inheritance: `ImageAsset`, `LinkAsset`, `ColorAsset`, `ColorGroupAsset`, `FolderAsset`. Virtual behavior: `HasPhysicalFile`, `CanHaveThumbnails`, `RequiresFileCleanup`. |
| `Models/Collection.cs` | � Refactored | **[Task #3]** Enums. **[Task #7]** Domain methods: `IsOwnedBy()`, `IsAccessibleBy()`, `ApplyUpdate()`. **[Task #8]** Navigation: `Assets`, `Parent`, `Children`. |
| `Models/Tag.cs` + `AssetTag.cs` | � Refactored | **[Task #7]** Domain methods: `SetName()` (auto-normalize), `UpdateFrom()`, `IsOwnedBy()`. |
| `Models/ApplicationUser.cs` | 🟡 Anemic | Kế thừa `IdentityUser` (OOP ✅), nhưng không mở rộng behavior nào. |
| `Models/CollectionPermission.cs` | � Refactored | **[Task #7]** Domain methods: `CanWrite`, `CanManage` (computed properties), `SetRole()` (validation). |
| `Models/Common.cs` | 🟢 OK | `PagedResult<T>` dùng generics tốt. `PaginationParams` có encapsulation (MaxPageSize). `FileUploadConfig` là config object hợp lý. |
| `Models/DTOs.cs` | 🟢 OK | DTO đúng chức năng — chỉ mang data, không cần behavior. |
| `Models/AuthDTOs.cs` | 🟢 OK | DTO thuần túy, phù hợp. |

**Vấn đề chính:**
- [x] **~~Asset là God Object~~**: ✅ **[Task #6]** Đã refactor thành TPH hierarchy (`ImageAsset`, `LinkAsset`, `ColorAsset`, `ColorGroupAsset`, `FolderAsset`) + `AssetFactory` + virtual behavior properties.
- [x] **~~Anemic Domain Model~~**: ✅ **[Task #7]** Đã thêm domain behavior cho `Asset` (`UpdatePosition`, `ApplyUpdate`, `SetThumbnails`, `MoveToFolder`, `MoveToCollection`), `Collection` (`IsOwnedBy`, `ApplyUpdate`, `IsAccessibleBy`), `Tag` (`SetName`, `UpdateFrom`), `CollectionPermission` (`CanWrite`, `CanManage`, `SetRole`).
- [x] **~~String-typed fields~~**: ✅ **[Task #3]** Đã chuyển `ContentType`, `Type`, `LayoutType` sang enums (`AssetContentType`, `CollectionType`, `LayoutType`) + EF Core value conversions.
- [x] **~~Thiếu navigation properties~~**: ✅ **[Task #8]** Đã thêm: `Asset` → `Collection`, `ParentFolder`. `Collection` → `Assets`, `Parent`, `Children`. EF Core configured với proper FK relationships.

### 2. Services — Refactored ✅

> **Phase 2 hoàn tất:** AssetService đã tách, Strategy pattern cho SmartCollection, AssetCleanupHelper extracted.

| File | Trạng thái | Vấn đề |
|------|-----------|--------|
| `IAssetService` / `AssetService` | 🟢 Refactored | **[Task 2.1,2.2]** Tách bulk ops → `IBulkAssetService`/`BulkAssetService`. Cleanup logic → `AssetCleanupHelper`. AssetService giờ ~280 dòng. |
| `IBulkAssetService` / `BulkAssetService` | 🟢 Mới | **[Task 2.1]** 4 bulk methods (delete, move, moveGroup, tag). ISP: tách khỏi IAssetService. |
| `AssetCleanupHelper` | 🟢 Mới | **[Task 2.2]** SRP: file + thumbnail cleanup. Dùng trong cả AssetService và BulkAssetService. |
| `IAuthService` / `AuthService` | 🟢 Tốt | Đúng abstraction, đúng SRP. Dùng DI tốt. |
| `ICollectionService` / `CollectionService` | 🟢 Tốt | DI, caching (IDistributedCache), delegation tốt. |
| `IStorageService` / `LocalStorageService` | 🟢 Tốt | **Tốt nhất project** — abstraction rõ ràng, dễ thay bằng S3/Azure implementation. Đúng OCP. |
| `IThumbnailService` / `ThumbnailService` | 🟢 Tốt | SRP, interface-based, clean. |
| `ITagService` / `TagService` | 🟡 Cần cải thiện | Hơi lớn (281 dòng), xử lý cả CRUD tag lẫn asset-tag junction logic. |
| `IPermissionService` / `PermissionService` | 🟢 Tốt | Logic rõ ràng, SRP. |
| `ISmartCollectionService` / `SmartCollectionService` | � Refactored | **[Task 2.4]** Strategy pattern: `ISmartCollectionFilter` interface + 5 concrete strategies (`RecentDaysFilter`, `ContentTypeFilter`, `UntaggedFilter`, `WithThumbnailsFilter`, `TagFilter`). OCP: thêm filter mới không cần sửa service. |
| `INotificationService` / `NotificationService` | 🟢 Tốt | SRP, abstraction tốt. |

**Điểm tốt (đã OOP):**
- ✅ **Dependency Injection** được dùng nhất quán qua tất cả services.
- ✅ **Interface-based programming**: Mọi service đều có interface → dễ test, dễ swap implementation.
- ✅ **IStorageService** là ví dụ tốt của OCP — `LocalStorageService` implement, dễ thêm `S3StorageService`.

**Vấn đề đã giải quyết (Phase 2):**
- [x] **~~AssetService quá lớn~~**: ✅ **[Task 2.1]** Tách `BulkAssetService` (4 bulk methods). AssetService giờ ~280 dòng.
- [x] **~~Duplicate cleanup logic~~**: ✅ **[Task 2.2]** `AssetCleanupHelper` class encapsulates file + thumbnail cleanup. DI-injected vào cả `AssetService` và `BulkAssetService`.
- [x] **~~SmartCollectionService hard-coded~~**: ✅ **[Task 2.4]** Strategy pattern: `ISmartCollectionFilter` + 5 concrete strategies + registry. OCP compliant.
- [x] **~~CollectionWithItemsResult~~** (trong `ICollectionService.cs`): Đã gom vào `Models/DTOs.cs`.
- [x] **~~SmartCollectionDefinition~~** (trong `ISmartCollectionService.cs`): Đã gom vào `Models/DTOs.cs`.

### 3. Controllers — Đạt tiêu chuẩn 🟢

| File | Trạng thái | Ghi chú |
|------|-----------|---------|
| `AssetsController` | 🟢 Tốt | Thin controller, delegate hết cho service. |
| `AuthController` | 🟢 Tốt | Clean, rate limiting attribute. |
| `CollectionsController` | 🟢 Tốt | Clean, thin. |
| `TagsController` | 🟢 Tốt | Clean, RESTful. |
| `PermissionsController` | 🟢 Tốt | Clean. |
| `SmartCollectionsController` | 🟢 Tốt | Clean, thin. |
| `SearchController` | � Refactored | **[Task #4]** Business logic đã move vào `SearchService`. Controller giờ chỉ delegate 1 dòng. |
| `HealthController` | 🟡 Chấp nhận | Logic health check đơn giản, có thể chấp nhận trong controller. |

**Vấn đề đã giải quyết:**
- [x] **~~SearchController chứa business logic~~**: ✅ **[Task #4]** Đã tạo `ISearchService` / `SearchService`. Controller giờ chỉ delegate.
- [x] **~~`AssetPositionDto` định nghĩa trong controller~~**: ✅ **[Task #1]** Đã move về `Models/DTOs.cs`.
- [x] **~~`SearchResult` class định nghĩa trong controller~~**: ✅ **[Task #1]** Đã move về `Models/DTOs.cs`.
- [x] **~~Duplicate `GetUserId()` helper~~**: ✅ **[Task #2]** Đã tạo `BaseApiController`. 8 controllers kế thừa.

### 4. Middleware & Hubs — Tốt 🟢

| File | Trạng thái | Ghi chú |
|------|-----------|---------|
| `ExceptionHandlingMiddleware` | 🟢 Tốt | Encapsulation tốt, extension method pattern. Dùng pattern matching cho exception mapping. |
| `AssetHub` | 🟢 Tốt | Kế thừa `Hub` (OOP tốt), clean override. |
| `HubEvents` | 🟡 OK | Static constants — có thể dùng enum tốt hơn nhưng chấp nhận được. |

### 5. Data Layer — Đạt 🟢

| File | Trạng thái | Ghi chú |
|------|-----------|---------|
| `AppDbContext` | 🟢 Tốt | Kế thừa `IdentityDbContext<ApplicationUser>`, Fluent API config tốt, dual-provider support qua `DatabaseProviderInfo`. |

### 6. Program.cs — Refactored ✅

- **Trạng thái:** **[Task #5]** Đã tạo `ServiceCollectionExtensions` với 6 extension methods. Program.cs giảm từ 255 dòng xuống 97 dòng.
- **Extension methods:** `AddCorsPolicy()`, `AddRateLimitingPolicies()`, `AddDatabase()`, `AddIdentityAndAuth()`, `AddCachingServices()`, `AddApplicationServices()`
- [x] ~~**Nên tạo ServiceCollectionExtensions**~~: ✅ Hoàn tất.

---

## II. FRONTEND (React / JavaScript)

### 1. API Layer — Refactored ✅

> **Phase 3 hoàn tất:** Tất cả API files đã được refactor thành class-based architecture với `BaseApiService` inheritance, `TokenManager` class, và barrel exports.

| File | Trạng thái | Ghi chú |
|------|-----------|--------|
| `api/TokenManager.js` | 🟢 Mới | **[Task 3.3]** Encapsulate JWT token persistence (`getToken`, `setToken`, `clearToken`, `hasToken`). Private field `#storageKey`. Singleton pattern. |
| `api/BaseApiService.js` | 🟢 Mới | **[Task 3.1]** Abstract base class: `_get()`, `_post()`, `_put()`, `_delete()` helpers. Subclasses chỉ cần set `endpoint`. OOP: Encapsulation, Inheritance, OCP. |
| `api/client.js` | 🟢 Refactored | **[Task 3.3]** Giờ dùng `TokenManager` singleton thay vì loose functions. Backward-compatible exports giữ nguyên. |
| `api/assetsApi.js` | 🟢 Refactored | **[Task 3.2]** `AssetApiService extends BaseApiService`. 13 methods. Backward-compatible named exports. |
| `api/authApi.js` | 🟢 Refactored | **[Task 3.2]** `AuthApiService extends BaseApiService`. |
| `api/collectionsApi.js` | 🟢 Refactored | **[Task 3.2]** `CollectionApiService extends BaseApiService`. |
| `api/tagsApi.js` | 🟢 Refactored | **[Task 3.2]** `TagApiService extends BaseApiService`. 10 methods. |
| `api/searchApi.js` | 🟢 Refactored | **[Task 3.2]** `SearchApiService extends BaseApiService`. |
| `api/smartCollectionsApi.js` | 🟢 Refactored | **[Task 3.2]** `SmartCollectionApiService extends BaseApiService`. |
| `api/permissionsApi.js` | 🟢 Refactored | **[Task 3.2, 3.4]** `PermissionApiService extends BaseApiService`. Style thống nhất (arrow → class methods). |
| `api/index.js` | 🟢 Mới | Barrel file — re-exports tất cả service singletons. |

**Vấn đề đã giải quyết:**
- [x] **~~Không có API class/object~~**: ✅ **[Task 3.2]** Mỗi file giờ là class kế thừa `BaseApiService`. Backward-compatible named exports giữ nguyên.
- [x] **~~Không có base API class~~**: ✅ **[Task 3.1]** `BaseApiService` với `_get/_post/_put/_delete` helpers.
- [x] **~~Inconsistent style~~**: ✅ **[Task 3.4]** Tất cả API files giờ dùng class methods (consistent style).
- [x] **~~Token management là loose functions~~**: ✅ **[Task 3.3]** `TokenManager` class with private fields + singleton.
- [ ] **Thiếu error handling chung**: Có thể thêm vào `BaseApiService` nếu cần (Phase sau).

### 2. Hooks — Functional (React idiom) 🟡

| File | Trạng thái | Ghi chú |
|------|-----------|---------|
| `hooks/useAuth.js` | 🟡 OK | `AuthProvider` + `useAuth` context pattern. Đúng React convention. Nhưng `persistUser` là nested function, nên là method của auth service. |
| `hooks/useAssets.js` | 🟡 Cần cải thiện | 276 dòng — quá lớn. Gom quá nhiều operations (upload, create folder/link/color/colorGroup, move, reorder, bulk ops, selection). |
| `hooks/useCollections.js` | 🟡 Cần cải thiện | 264 dòng — lớn. Gom fetch, select, navigate, create, delete, URL sync. |
| `hooks/useTags.js` | 🟢 OK | Vừa phải, SRP tốt. |
| `hooks/useSignalR.js` | 🟢 OK | Clean, focused. |
| `hooks/useUndoRedo.js` | 🟢 Tốt | **Ví dụ tốt nhất FE** — Command pattern (OOP design pattern). Mỗi command có `execute()` và `undo()`. |

**Vấn đề chính:**
- [ ] **useAssets.js quá lớn**: Nên tách thành nhiều hooks chuyên biệt (`useAssetSelection`, `useAssetUpload`, `useAssetBulkOps`...).
- [ ] **useCollections.js quá lớn**: Tương tự, nên tách (`useCollectionCRUD`, `useCollectionNavigation`).
- [ ] **Không có domain model/class ở FE**: Data từ API được dùng trực tiếp dạng plain object. Không có class `Asset`, `Collection`, `Tag` ở frontend → thiếu type safety, thiếu computed properties, thiếu validation.

### 3. Components — Mostly Functional 🟡

| File | Trạng thái | Ghi chú |
|------|-----------|---------|
| `ErrorBoundary.jsx` | 🟢 Class Component | **Duy nhất dùng class** — React bắt buộc cho Error Boundary. OOP đúng cách. |
| `App.jsx` | 🔴 God Component | 615 dòng — chứa gần như toàn bộ app logic. Quá nhiều state, quá nhiều handlers. Vi phạm SRP. |
| `AssetDisplayer.jsx` | 🟢 OK | Functional component, nhỏ, focused. |
| `AssetGrid.jsx` | 🟢 OK | Nhỏ, presentational. |
| `CollectionBrowser.jsx` | 🟡 OK | 168 dòng, hơi lớn nhưng chấp nhận. |
| `CollectionTree.jsx` | 🟢 OK | Recursive rendering (tree pattern). |
| `ColorBoard.jsx` | 🟢 OK | Focused. |
| `DraggableAssetCanvas.jsx` | 🟡 OK | Logic drag-n-drop inline, có thể extract. |
| `LoginPage.jsx` | 🟢 OK | Clean, focused. |
| `SearchBar.jsx` | 🟢 OK | Rất nhỏ, presentational. |
| `ShareDialog.jsx` | 🟡 OK | Có side effects trong component, nên move logic ra hook. |
| `UploadArea.jsx` | 🟢 OK | Clean, dùng `react-dropzone` đúng cách. |

**Vấn đề chính:**
- [ ] **App.jsx là God Component (615 dòng)**: Chứa state management, business logic, layout, routing logic tất cả trong 1 file. Cần tách thành nhiều component + context.
- [ ] **Không có state management pattern**: Không dùng Redux, Zustand, hay Context API (ngoài Auth). Toàn bộ state gom ở App.jsx rồi prop-drill xuống.
- [ ] **Thiếu component hierarchy rõ ràng**: Không có Layout component, không có Page component pattern.

---

## III. BẢNG TỔNG KẾT NGUYÊN TẮC OOP

| Nguyên tắc | Backend | Frontend | Ghi chú |
|------------|---------|----------|---------|
| **Encapsulation** | 🟢 Tốt | 🟡 Cải thiện | BE: Domain methods, private/virtual behavior, factory pattern. FE: `TokenManager` private fields, class-based API services |
| **Abstraction** | 🟢 Tốt | 🟡 Cải thiện | BE: Interface-based DI. FE: `BaseApiService` abstraction layer |
| **Inheritance** | 🟢 Tốt | 🟢 Tốt | BE: TPH hierarchy, BaseApiController. FE: `BaseApiService` → 7 subclasses |
| **Polymorphism** | 🟢 Tốt | 🟡 Cơ bản | BE: IStorageService, virtual behavior. FE: Override `_get/_post/_put/_delete` khi cần |

### SOLID Principles

| Nguyên tắc | Backend | Frontend |
|------------|---------|----------|
| **S** - Single Responsibility | � | 🔴 (App.jsx, useAssets.js, useCollections.js) |
| **O** - Open/Closed | 🟢 | � (BaseApiService extensible) |
| **L** - Liskov Substitution | 🟢 | 🟢 (API subclasses substitutable) |
| **I** - Interface Segregation | � (IBulkAssetService tách) | 🟡 (mỗi API service focus 1 domain) |
| **D** - Dependency Inversion | 🟢 | 🟡 (singleton services, nhưng chưa có DI container) |

### Design Patterns Đã Dùng

| Pattern | Nơi dùng | Đánh giá |
|---------|----------|----------|
| Repository (ngầm qua EF) | Backend DbContext | 🟢 |
| Dependency Injection | Backend Services | 🟢 |
| Strategy (IStorageService) | Backend Storage | 🟢 |
| Command Pattern | Frontend useUndoRedo | 🟢 |
| Observer (SignalR) | Backend/Frontend real-time | 🟢 |
| Factory Pattern | Backend AssetFactory | 🟢 |
| Type Hierarchy / TPH | Backend Asset → 5 subtypes | 🟢 |
| Extension Methods | Backend ServiceCollectionExtensions | 🟢 |
| **Strategy Pattern** | SmartCollection filters | Thay thế switch statement cứng |
| **Facade Pattern** | Frontend API layer | Gom các loose functions thành class |
| **Service Layer (FE)** | Frontend business logic | Tách logic khỏi hooks/components |
| **Value Object** | ContentType, Role, LayoutType | Thay string bằng type-safe objects |

### Design Patterns Đã Áp Dụng (sau refactor)

| Pattern | Nơi dùng | Task |
|---------|----------|------|
| **TPH Inheritance** | `Asset` → `ImageAsset`, `LinkAsset`, `ColorAsset`, `ColorGroupAsset`, `FolderAsset` | Task #6 |
| **Factory Pattern** | `AssetFactory` — 6 static creation methods | Task #6 |
| **Template Method** (virtual) | `HasPhysicalFile`, `CanHaveThumbnails`, `RequiresFileCleanup` virtual properties | Task #6 |
| **Value Objects** (Enums) | `AssetContentType`, `CollectionType`, `LayoutType` + EF Core value conversions | Task #3 |
| **Extension Methods** | `ServiceCollectionExtensions` — 6 methods tổ chức DI | Task #5 |
| **Base Controller** | `BaseApiController` — shared `GetUserId()` | Task #2 |
| **Rich Domain Model** | Domain methods trên Asset, Collection, Tag, CollectionPermission | Task #7 |
| **BaseApiService (FE)** | Abstract base class → 7 API service subclasses kế thừa | Task 3.1, 3.2 |
| **Singleton (FE)** | `TokenManager` singleton instance, `*ApiService` singletons | Task 3.3 |
| **Encapsulation (FE)** | `TokenManager` private `#storageKey` field | Task 3.3 |
| **Barrel Exports (FE)** | `api/index.js` — unified service re-exports | Task 3.2 |
| **Strategy Pattern** | `ISmartCollectionFilter` + 5 concrete strategies (`RecentDaysFilter`, `ContentTypeFilter`, `UntaggedFilter`, `WithThumbnailsFilter`, `TagFilter`) | Task 2.4 |
| **Helper/Utility Class** | `AssetCleanupHelper` — encapsulate file + thumbnail cleanup | Task 2.2 |
| **Interface Segregation** | `IBulkAssetService` tách khỏi `IAssetService` | Task 2.1 |

---

## IV. KẾ HOẠCH REFACTOR (ĐỀ XUẤT THỨ TỰ ƯU TIÊN)

### Phase 1: Backend Models — Nền tảng ✅
> Hoàn tất 2026-02-27.

- [x] 1.1 Tạo Asset inheritance hierarchy (`Asset` → `ImageAsset`, `LinkAsset`, `ColorAsset`, `FolderAsset`, `ColorGroupAsset`)
- [x] 1.2 Chuyển string fields sang enum/value objects (`ContentType`, `CollectionType`, `LayoutType`, `Role`)
- [x] 1.3 Thêm domain behavior vào models (validation, computed properties)
- [x] 1.4 Thêm navigation properties đầy đủ
- [x] 1.5 Move `CollectionWithItemsResult`, `SmartCollectionDefinition`, `SearchResult`, `AssetPositionDto` về Models folder

### Phase 2: Backend Services — Tách nhỏ ✅
> Hoàn tất 2026-02-28.

- [x] 2.1 Tách `AssetService` → `BulkAssetService` (4 bulk methods). AssetService giờ ~280 dòng.
- [x] 2.2 Extract `AssetCleanupHelper` (file + thumbnail cleanup). DI-registered.
- [x] 2.3 Tạo `ISearchService` / `SearchService`, move logic khỏi `SearchController`
- [x] 2.4 Áp dụng Strategy pattern cho `SmartCollectionService` (5 strategies)
- [x] 2.5 Tạo base controller class (extract `GetUserId()`)
- [x] 2.6 Tạo `ServiceCollectionExtensions` cho Program.cs DI registration

### Phase 3: Frontend API Layer — OOP hóa ✅
> Hoàn tất 2026-02-28.

- [x] 3.1 Tạo `BaseApiService` class (chứa common logic: `_get/_post/_put/_delete` helpers)
- [x] 3.2 Tạo các API class kế thừa: `AssetApiService`, `CollectionApiService`, `TagApiService`, etc. (7 classes)
- [x] 3.3 Tạo `TokenManager` class (encapsulate localStorage, private fields, singleton)
- [x] 3.4 Thống nhất style — tất cả dùng class methods, không còn mix async function/arrow

### Phase 4: Frontend Domain Models ✅
> Hoàn tất 2026-02-28.

- [x] 4.1 Tạo frontend domain classes: `Asset`, `Collection`, `Tag` (với computed properties)
- [x] 4.2 Thêm computed properties, validation logic vào domain classes
- [x] 4.3 Mapping layer: `toAsset()`, `toCollection()`, `toTag()` + array variants

### Phase 5: Frontend Component Architecture ✅
> Hoàn tất 2026-02-28.

- [x] 5.1 Tách `App.jsx` (620→454 dòng): `AppHeader`, `AppSidebar`, `DetailsPanel` components + `useSmartCollections` hook
- [x] 5.2 Tách `useAssets.js`: `useAssetSelection` (multi-select) + `useBulkOperations` (bulk delete/move/tag)
- [x] 5.3 Tách `useCollections.js`: `useCollectionNavigation` (URL sync, breadcrumbs, folder path)
- [ ] 5.4 Thiết lập state management pattern (Context API hoặc Zustand)
- [ ] 5.5 Extract business logic từ `ShareDialog` ra hook riêng

---

## V. THEO DÕI TIẾN TRÌNH

| # | Task | Trạng thái | Ngày bắt đầu | Ngày hoàn tất | Ghi chú |
|---|------|-----------|--------------|---------------|---------|
| 1.5 | Move DTOs/Result classes | ✅ Hoàn tất | 2026-02-27 | 2026-02-27 | `AssetPositionDto`, `SearchResult`, `CollectionWithItemsResult`, `SmartCollectionDefinition` → `Models/DTOs.cs` |
| 2.5 | Base controller class | ✅ Hoàn tất | 2026-02-27 | 2026-02-27 | `BaseApiController` với `GetUserId()`. 8 controllers kế thừa. |
| 1.2 | Enum/Value Objects cho string fields | ✅ Hoàn tất | 2026-02-27 | 2026-02-27 | `AssetContentType`, `CollectionType`, `LayoutType` enums + EF Core value conversions. |
| 2.3 | SearchService | ✅ Hoàn tất | 2026-02-27 | 2026-02-27 | `ISearchService` + `SearchService`. SearchController giờ chỉ delegate. |
| 2.6 | ServiceCollectionExtensions | ✅ Hoàn tất | 2026-02-27 | 2026-02-27 | `Extensions/ServiceCollectionExtensions.cs` — 6 extension methods. Program.cs 255→148 lines. |
| 1.1 | Asset inheritance hierarchy | ✅ Hoàn tất | 2026-02-27 | 2026-02-27 | TPH: `ImageAsset`, `LinkAsset`, `ColorAsset`, `ColorGroupAsset`, `FolderAsset`. `AssetFactory` + virtual behavior properties. |
| 1.3 | Domain behavior cho models | ✅ Hoàn tất | 2026-02-27 | 2026-02-27 | Asset: 6 domain methods. Collection: 3 methods. Tag: 3 methods. CollectionPermission: 2 computed + 1 method. Services updated to use domain methods. |
| 1.4 | Navigation properties | ✅ Hoàn tất | 2026-02-27 | 2026-02-27 | Asset→Collection, ParentFolder. Collection→Assets, Parent, Children. EF Core configured. |
| 2.1 | Tách AssetService | ✅ Hoàn tất | 2026-02-28 | 2026-02-28 | `IBulkAssetService`/`BulkAssetService`: 4 bulk methods. `AssetService` giảm từ ~480 xuống ~280 dòng. Controller inject cả 2 services. |
| 2.2 | Extract reusable helpers | ✅ Hoàn tất | 2026-02-28 | 2026-02-28 | `AssetCleanupHelper` class: `CleanupFilesAsync`, `CleanupThumbnailsAsync`. DI-registered, dùng trong cả AssetService và BulkAssetService. |
| 2.4 | Strategy pattern SmartCollection | ✅ Hoàn tất | 2026-02-28 | 2026-02-28 | `ISmartCollectionFilter` interface + 5 strategies: `RecentDaysFilter`, `ContentTypeFilter`, `UntaggedFilter`, `WithThumbnailsFilter`, `TagFilter`. Registry lookup thay switch. |
| 3.1 | BaseApiService class | ✅ Hoàn tất | 2026-02-28 | 2026-02-28 | `BaseApiService` với `_get/_post/_put/_delete`. Tất cả 7 API services kế thừa. |
| 3.2 | API classes kế thừa | ✅ Hoàn tất | 2026-02-28 | 2026-02-28 | 7 classes: `AssetApiService`, `AuthApiService`, `CollectionApiService`, `TagApiService`, `SearchApiService`, `SmartCollectionApiService`, `PermissionApiService`. Backward-compatible named exports. |
| 3.3 | TokenManager class | ✅ Hoàn tất | 2026-02-28 | 2026-02-28 | `TokenManager` class, private `#storageKey`, singleton pattern. `client.js` cập nhật dùng `TokenManager`. |
| 3.4 | Thống nhất code style | ✅ Hoàn tất | 2026-02-28 | 2026-02-28 | Tất cả API files giờ dùng class methods, consistent style. `permissionsApi.js` không còn dùng `async function` declarations riêng lẻ. |
| 4.1 | Frontend domain classes | ✅ Hoàn tất | 2026-02-28 | 2026-02-28 | `Asset`, `Collection`, `Tag` classes trong `models/index.js`. Computed: `isImage`, `thumbnailUrl`, `tagList`, `contentTypeLabel`, `badgeStyle`, etc. |
| 4.2 | Computed properties/validation | ✅ Hoàn tất | 2026-02-28 | 2026-02-28 | Kết hợp trong 4.1. Mỗi class có `isValid` getter. `Tag` có private `#contrastColor()`. |
| 4.3 | API → Domain mapping | ✅ Hoàn tất | 2026-02-28 | 2026-02-28 | `toAsset/toAssets`, `toCollection/toCollections`, `toTag/toTags` helpers. Idempotent (skip nếu đã là instance). |
| 5.1 | Tách App.jsx | ✅ Hoàn tất | 2026-02-28 | 2026-02-28 | 620→454 dòng. `AppHeader`, `AppSidebar`, `DetailsPanel` components + `useSmartCollections` hook extracted. |
| 5.2 | Tách useAssets.js | ✅ Hoàn tất | 2026-02-28 | 2026-02-28 | `useAssetSelection` (selection state, toggle, range, selectAll). `useBulkOperations` (bulk delete/move/tag/moveGroup). `useAssets` giờ compose 2 hooks. |
| 5.3 | Tách useCollections.js | ✅ Hoàn tất | 2026-02-28 | 2026-02-28 | `useCollectionNavigation` (URL push, breadcrumbs, folder nav, syncFromUrl/syncInitial). `useCollections` compose hook + keeps fetch/CRUD. |
| 5.4 | State management pattern | ⬜ Chưa bắt đầu | | | |
| 5.5 | Extract ShareDialog logic | ⬜ Chưa bắt đầu | | | |

---

## VI. CHÚ THÍCH

### Ký hiệu đánh giá
- 🟢 **Tốt** — Đã áp dụng OOP đúng cách
- 🟡 **Trung bình** — Có OOP nhưng cần cải thiện
- 🔴 **Cần refactor** — Chưa OOP hoặc vi phạm nguyên tắc nghiêm trọng
- ⬜ Chưa bắt đầu | 🔄 Đang làm | ✅ Hoàn tất

### Lưu ý quan trọng
1. **Không phá vỡ API contract**: Khi refactor backend, giữ nguyên API endpoints/response format để frontend không bị ảnh hưởng.
2. **Refactor dần dần**: Mỗi phase nên test kỹ trước khi chuyển sang phase tiếp theo.
3. **React convention**: Ở frontend, functional components + hooks là convention (không bắt buộc class). OOP ở FE tập trung vào API layer, domain models, và service classes — không phải ở components.
4. **EF Core TPH**: Khi tạo Asset hierarchy, dùng Table-Per-Hierarchy (TPH) mapping để tương thích với DB hiện tại.
