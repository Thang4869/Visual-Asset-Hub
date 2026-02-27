# Bản Đánh Giá OOP - Visual Asset Hub (VAH)

> **Ngày tạo:** 2026-02-27  
> **Mục đích:** Đánh giá mức độ áp dụng OOP trong toàn bộ dự án, làm cơ sở cho quá trình refactor.  
> **Trạng thái:** 🔴 Đang tiến hành

---

## Tổng Quan Dự Án

| Layer | Ngôn ngữ | Framework |
|-------|----------|-----------|
| Backend | C# (.NET 9) | ASP.NET Core, EF Core, Identity, SignalR |
| Frontend | JavaScript (JSX) | React 18, Vite, Axios |

---

## I. BACKEND (C# / .NET)

### 1. Models — Anemic Domain Models 🟡

| File | Trạng thái | Vấn đề |
|------|-----------|--------|
| `Models/Asset.cs` | � Refactored | **[Task #3]** `ContentType` đã chuyển sang enum `AssetContentType`. **[Task #6]** TPH inheritance: `ImageAsset`, `LinkAsset`, `ColorAsset`, `ColorGroupAsset`, `FolderAsset`. Virtual behavior: `HasPhysicalFile`, `CanHaveThumbnails`, `RequiresFileCleanup`. |
| `Models/Collection.cs` | 🟡 Anemic | Chỉ có properties, không có domain logic. |
| `Models/Tag.cs` + `AssetTag.cs` | 🟡 Anemic | Chỉ có properties. |
| `Models/ApplicationUser.cs` | 🟡 Anemic | Kế thừa `IdentityUser` (OOP ✅), nhưng không mở rộng behavior nào. |
| `Models/CollectionPermission.cs` | 🟡 Anemic | Chỉ có properties. `CollectionRoles` static class có logic nhưng tách rời khỏi entity. |
| `Models/Common.cs` | 🟢 OK | `PagedResult<T>` dùng generics tốt. `PaginationParams` có encapsulation (MaxPageSize). `FileUploadConfig` là config object hợp lý. |
| `Models/DTOs.cs` | 🟢 OK | DTO đúng chức năng — chỉ mang data, không cần behavior. |
| `Models/AuthDTOs.cs` | 🟢 OK | DTO thuần túy, phù hợp. |

**Vấn đề chính:**
- [x] **~~Asset là God Object~~**: ✅ **[Task #6]** Đã refactor thành TPH hierarchy (`ImageAsset`, `LinkAsset`, `ColorAsset`, `ColorGroupAsset`, `FolderAsset`) + `AssetFactory` + virtual behavior properties.
- [ ] **Anemic Domain Model**: Tất cả các model đều là "data bag" — không có domain method nào. Toàn bộ business logic nằm ở Service layer. Thiếu encapsulation.
- [x] **~~String-typed fields~~**: ✅ **[Task #3]** Đã chuyển `ContentType`, `Type`, `LayoutType` sang enums (`AssetContentType`, `CollectionType`, `LayoutType`) + EF Core value conversions.
- [ ] **Thiếu navigation properties đầy đủ**: `Asset` không có navigation đến `Collection`, `ApplicationUser`. `Collection` thiếu navigation đến children, assets.

### 2. Services — Chấp nhận được nhưng cần cải thiện 🟡

| File | Trạng thái | Vấn đề |
|------|-----------|--------|
| `IAssetService` / `AssetService` | 🟡 Cần cải thiện | Interface + implementation tốt (DI, abstraction). Nhưng class quá lớn (518 dòng), vi phạm SRP — xử lý upload, CRUD, folder, color, link, bulk ops trong 1 class. |
| `IAuthService` / `AuthService` | 🟢 Tốt | Đúng abstraction, đúng SRP. Dùng DI tốt. |
| `ICollectionService` / `CollectionService` | 🟢 Tốt | DI, caching (IDistributedCache), delegation tốt. |
| `IStorageService` / `LocalStorageService` | 🟢 Tốt | **Tốt nhất project** — abstraction rõ ràng, dễ thay bằng S3/Azure implementation. Đúng OCP. |
| `IThumbnailService` / `ThumbnailService` | 🟢 Tốt | SRP, interface-based, clean. |
| `ITagService` / `TagService` | 🟡 Cần cải thiện | Hơi lớn (281 dòng), xử lý cả CRUD tag lẫn asset-tag junction logic. |
| `IPermissionService` / `PermissionService` | 🟢 Tốt | Logic rõ ràng, SRP. |
| `ISmartCollectionService` / `SmartCollectionService` | 🟡 Cần cải thiện | Dùng chuỗi if/switch cho filter — thiếu Strategy pattern. Hard-coded definitions. |
| `INotificationService` / `NotificationService` | 🟢 Tốt | SRP, abstraction tốt. |

**Điểm tốt (đã OOP):**
- ✅ **Dependency Injection** được dùng nhất quán qua tất cả services.
- ✅ **Interface-based programming**: Mọi service đều có interface → dễ test, dễ swap implementation.
- ✅ **IStorageService** là ví dụ tốt của OCP — `LocalStorageService` implement, dễ thêm `S3StorageService`.

**Vấn đề cần refactor:**
- [ ] **AssetService quá lớn (~480 dòng)**: Nên tách thành các service chuyên biệt: `FileUploadService`, `FolderService`, `ColorService`, `LinkService`, `BulkOperationService`.
- [x] **~~Duplicate cleanup logic~~**: ✅ **[Task #6]** Cleanup logic giờ dùng `asset.RequiresFileCleanup` (virtual property) thay vì duplicate if/else chain trong cả `DeleteAssetAsync` và `BulkDeleteAsync`.
- [ ] **SmartCollectionService hard-coded**: Các smart collection definitions được hard-code trong method. Nên dùng Strategy/Registry pattern.
- [ ] **CollectionWithItemsResult** (trong `ICollectionService.cs`): DTO nên ở Models, không ở interface file.
- [ ] **SmartCollectionDefinition** (trong `ISmartCollectionService.cs`): Model nên ở Models folder.

### 3. Controllers — Đạt tiêu chuẩn 🟢

| File | Trạng thái | Ghi chú |
|------|-----------|---------|
| `AssetsController` | 🟢 Tốt | Thin controller, delegate hết cho service. |
| `AuthController` | 🟢 Tốt | Clean, rate limiting attribute. |
| `CollectionsController` | 🟢 Tốt | Clean, thin. |
| `TagsController` | 🟢 Tốt | Clean, RESTful. |
| `PermissionsController` | 🟢 Tốt | Clean. |
| `SmartCollectionsController` | 🟢 Tốt | Clean, thin. |
| `SearchController` | 🔴 Vấn đề | **Có business logic trực tiếp trong controller** — query DB, filter, paginate. Thiếu `ISearchService`. Vi phạm SRP. |
| `HealthController` | 🟡 Chấp nhận | Logic health check đơn giản, có thể chấp nhận trong controller. |

**Vấn đề cần refactor:**
- [ ] **SearchController chứa business logic**: Toàn bộ search logic (119 dòng) nằm trong controller thay vì service. Cần tạo `ISearchService` / `SearchService`.
- [ ] **`AssetPositionDto` định nghĩa trong controller file**: Nên move về Models/DTOs.cs.
- [ ] **`SearchResult` class định nghĩa trong controller file**: Nên move về Models.
- [ ] **Duplicate `GetUserId()` helper**: Copy-paste ở 5 controllers. Nên tạo base controller class.

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

### 6. Program.cs — Procedural 🟡

- **Trạng thái:** Procedural configuration (255 dòng, top-level statements).  
- **Ghi chú:** Đây là chuẩn .NET minimal hosting — không cần OOP hóa, nhưng có thể tổ chức tốt hơn bằng extension methods (ví dụ: `builder.Services.AddApplicationServices()`, `builder.Services.AddAuthenticationConfig()`).
- [ ] **Nên tạo ServiceCollectionExtensions** để gom DI registration.

---

## II. FRONTEND (React / JavaScript)

### 1. API Layer — Procedural Functions 🔴

| File | Trạng thái | Vấn đề |
|------|-----------|--------|
| `api/client.js` | 🟡 Module | Axios instance + interceptors. Functional, không OOP. Các helper (`getToken`, `setToken`, `clearToken`, `staticUrl`) là loose functions. |
| `api/assetsApi.js` | 🔴 Procedural | Các function rời rạc, không có class hay object. |
| `api/authApi.js` | 🔴 Procedural | Tương tự. |
| `api/collectionsApi.js` | 🔴 Procedural | Tương tự. |
| `api/tagsApi.js` | 🔴 Procedural | Tương tự. |
| `api/searchApi.js` | 🔴 Procedural | Tương tự. |
| `api/smartCollectionsApi.js` | 🔴 Procedural | Tương tự. |
| `api/permissionsApi.js` | 🔴 Procedural | Tương tự, nhưng dùng async function declarations (hơi khác style so với các file khác dùng arrow). |

**Vấn đề chính:**
- [ ] **Không có API class/object**: Mỗi file export nhiều function rời rạc. Nên gom thành class (ví dụ: `class AssetApi { ... }`) hoặc ít nhất là object gom nhóm.
- [ ] **Không có base API class**: Mọi file tự import `apiClient` rồi gọi trực tiếp. Nên có abstract base → kế thừa.
- [ ] **Inconsistent style**: `permissionsApi.js` dùng `async function` declarations, các file khác dùng arrow functions. Thiếu consistency.
- [ ] **Token management (`getToken`/`setToken`/`clearToken`)**: Nên encapsulate thành `TokenManager` class hoặc `AuthStorage` class.
- [ ] **Thiếu error handling chung**: Mỗi API call tự handle error khác nhau.

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
| **Encapsulation** | 🟡 Trung bình | 🔴 Yếu | BE: Models expose hết, FE: Dùng plain objects |
| **Abstraction** | 🟢 Tốt | 🔴 Yếu | BE: Interface-based DI, FE: Không có abstraction layer |
| **Inheritance** | 🟡 Hạn chế | 🔴 Không dùng | BE: Chỉ ApplicationUser extends IdentityUser, Asset nên có hierarchy |
| **Polymorphism** | 🟡 Hạn chế | 🔴 Không dùng | BE: IStorageService tốt, nhưng Asset dùng if/switch thay vì polymorphism |

### SOLID Principles

| Nguyên tắc | Backend | Frontend |
|------------|---------|----------|
| **S** - Single Responsibility | 🟡 | 🔴 (App.jsx, useAssets.js, useCollections.js) |
| **O** - Open/Closed | 🟡 | 🔴 |
| **L** - Liskov Substitution | 🟢 | N/A |
| **I** - Interface Segregation | 🟡 (IAssetService quá lớn) | 🔴 |
| **D** - Dependency Inversion | 🟢 | 🔴 (direct import, no DI) |

### Design Patterns Đã Dùng

| Pattern | Nơi dùng | Đánh giá |
|---------|----------|----------|
| Repository (ngầm qua EF) | Backend DbContext | 🟢 |
| Dependency Injection | Backend Services | 🟢 |
| Strategy (IStorageService) | Backend Storage | 🟢 |
| Command Pattern | Frontend useUndoRedo | 🟢 |
| Observer (SignalR) | Backend/Frontend real-time | 🟢 |
| Middleware Pipeline | Backend ExceptionHandling | 🟢 |

### Design Patterns Nên Áp Dụng

| Pattern | Nơi nên dùng | Lý do |
|---------|-------------|-------|
| **Type Hierarchy / Inheritance** | Asset → ImageAsset, LinkAsset, ColorAsset, FolderAsset | Thay thế if/switch trên ContentType |
| **Factory Pattern** | Asset creation | Tạo đúng subtype dựa trên input |
| **Strategy Pattern** | SmartCollection filters | Thay thế switch statement cứng |
| **Facade Pattern** | Frontend API layer | Gom các loose functions thành class |
| **Service Layer (FE)** | Frontend business logic | Tách logic khỏi hooks/components |
| **Value Object** | ContentType, Role, LayoutType | Thay string bằng type-safe objects |

---

## IV. KẾ HOẠCH REFACTOR (ĐỀ XUẤT THỨ TỰ ƯU TIÊN)

### Phase 1: Backend Models — Nền tảng 🔴
> Ưu tiên cao nhất vì models ảnh hưởng toàn bộ codebase.

- [ ] 1.1 Tạo Asset inheritance hierarchy (`Asset` → `ImageAsset`, `LinkAsset`, `ColorAsset`, `FolderAsset`, `ColorGroupAsset`)
- [ ] 1.2 Chuyển string fields sang enum/value objects (`ContentType`, `CollectionType`, `LayoutType`, `Role`)
- [ ] 1.3 Thêm domain behavior vào models (validation, computed properties)
- [ ] 1.4 Thêm navigation properties đầy đủ
- [ ] 1.5 Move `CollectionWithItemsResult`, `SmartCollectionDefinition`, `SearchResult`, `AssetPositionDto` về Models folder

### Phase 2: Backend Services — Tách nhỏ 🟡
> Ưu tiên trung bình, phụ thuộc vào Phase 1.

- [ ] 2.1 Tách `AssetService` → `FileUploadService`, `FolderService`, `ColorService`, `LinkService`, `BulkOperationService`
- [ ] 2.2 Extract tái sử dụng (file cleanup, thumbnail cleanup) thành helper class
- [ ] 2.3 Tạo `ISearchService` / `SearchService`, move logic khỏi `SearchController`
- [ ] 2.4 Áp dụng Strategy pattern cho `SmartCollectionService`
- [ ] 2.5 Tạo base controller class (extract `GetUserId()`)
- [ ] 2.6 Tạo `ServiceCollectionExtensions` cho Program.cs DI registration

### Phase 3: Frontend API Layer — OOP hóa 🔴
> Ưu tiên cao ở frontend.

- [ ] 3.1 Tạo `BaseApiService` class (chứa common logic: error handling, auth header)
- [ ] 3.2 Tạo các API class kế thừa: `AssetApiService`, `CollectionApiService`, `TagApiService`, etc.
- [ ] 3.3 Tạo `TokenManager` / `AuthStorage` class (encapsulate localStorage)
- [ ] 3.4 Thống nhất style (async function vs arrow)

### Phase 4: Frontend Domain Models 🟡
> Ưu tiên trung bình.

- [ ] 4.1 Tạo frontend domain classes: `Asset`, `Collection`, `Tag`, `User`
- [ ] 4.2 Thêm computed properties, validation logic vào domain classes
- [ ] 4.3 Mapping layer: API response → domain object

### Phase 5: Frontend Component Architecture 🟡
> Ưu tiên trung bình, thực hiện dần.

- [ ] 5.1 Tách `App.jsx` thành nhiều Page/Layout components
- [ ] 5.2 Tách `useAssets.js` thành nhiều hooks chuyên biệt
- [ ] 5.3 Tách `useCollections.js` tương tự
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
| 2.6 | ServiceCollectionExtensions | ✅ Hoàn tất | 2026-02-27 | 2026-02-27 | `Extensions/ServiceCollectionExtensions.cs` — 5 extension methods. Program.cs 255→116 lines. |
| 1.1 | Asset inheritance hierarchy | ✅ Hoàn tất | 2026-02-27 | 2026-02-27 | TPH: `ImageAsset`, `LinkAsset`, `ColorAsset`, `ColorGroupAsset`, `FolderAsset`. `AssetFactory` + virtual behavior properties. |
| 1.3 | Domain behavior cho models | ⬜ Chưa bắt đầu | | | |
| 1.4 | Navigation properties | ⬜ Chưa bắt đầu | | | |
| 2.1 | Tách AssetService | ⬜ Chưa bắt đầu | | | |
| 2.2 | Extract reusable helpers | ⬜ Chưa bắt đầu | | | |
| 2.4 | Strategy pattern SmartCollection | ⬜ Chưa bắt đầu | | | |
| 3.1 | BaseApiService class | ⬜ Chưa bắt đầu | | | |
| 3.2 | API classes kế thừa | ⬜ Chưa bắt đầu | | | |
| 3.3 | TokenManager class | ⬜ Chưa bắt đầu | | | |
| 3.4 | Thống nhất code style | ⬜ Chưa bắt đầu | | | |
| 4.1 | Frontend domain classes | ⬜ Chưa bắt đầu | | | |
| 4.2 | Computed properties/validation | ⬜ Chưa bắt đầu | | | |
| 4.3 | API → Domain mapping | ⬜ Chưa bắt đầu | | | |
| 5.1 | Tách App.jsx | ⬜ Chưa bắt đầu | | | |
| 5.2 | Tách useAssets.js | ⬜ Chưa bắt đầu | | | |
| 5.3 | Tách useCollections.js | ⬜ Chưa bắt đầu | | | |
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
