# Visual Asset Hub — Đánh giá Kiến trúc & Lộ trình Phát triển

> **Cập nhật lần cuối:** 02/03/2026  
> **Phiên bản:** 6.1 — Đồng bộ kiến trúc Feature Slice cho Assets (Command/Query/Application/Infrastructure)

---

## Executive Summary

### System Goal

Visual Asset Hub (VAH) is a modern web application for managing digital assets including images, links, and color palettes. It provides a dark-themed interface with drag-and-drop functionality, hierarchical collections, many-to-many tagging, role-based access control (RBAC) for sharing, and real-time synchronization across devices. The system supports both individual creators and small teams, with production-ready features like file upload restrictions, thumbnails, search, and bulk operations.

### Current Architecture in 5 Bullets

- **Client:** React 19 Single Page Application (SPA) built with Vite 7, featuring a dark navy theme, drag-and-drop uploads, and responsive grid/masonry views.
- **Backend:** ASP.NET Core 9 API with endpoints (see Swagger), using Entity Framework Core for data access, SignalR for real-time updates, and a service layer with dependency injection.
- **Database:** Dual-provider support for SQLite (development) and PostgreSQL (production), with entity tables and indexes (see EF migrations).
- **Cache & Storage:** Redis for distributed caching (with in-memory fallback), local file storage for uploads and auto-generated thumbnails.
- **Real-time:** SignalR WebSocket hub for live notifications, scoped to user groups for security.

### Top 5 Architecture Decisions

- **Feature Slice Pattern:** Separated Assets into Command, Query, and Application layers for better maintainability and testability.
- **User-Scoped Data Isolation:** All entities include UserId foreign keys, enforced at service layer to prevent data leakage.
- **Dual Database Provider:** Conditional EF Core configuration for SQLite/PostgreSQL to support dev/prod environments seamlessly.
- **Service Layer with Interfaces:** Services abstracted behind interfaces for clean dependency injection and mockability.
- **Docker Multi-Stage Builds:** Optimized container images with separate build/runtime stages, non-root users, and health checks.

### Top 5 Remaining Risks + Status

- **XSS / URL Injection (MEDIUM):** Potential malicious links in asset URLs; [see section 2.1](#21-bảo-mật-security).
- **Optimistic Concurrency (MEDIUM):** No RowVersion on entities, risking silent overwrites; [see section 2.3](#23-database--dữ-liệu).
- **Horizontal Scaling (HIGH):** SQLite limits to single instance; requires PostgreSQL + shared storage; [see section 2.4](#24-storage--mở-rộng).
- **HTTPS Enforcement (MEDIUM):** Default HTTP in dev; needs redirect/HSTS in prod; [see section 2.6](#26-devops--production-readiness).
- **Frontend Environment Separation (MEDIUM):** Hardcoded API_URL; needs VITE_API_URL per env; [see section 2.5](#25-frontend-architecture).

## 1. Tổng quan hệ thống

Visual Asset Hub (VAH) là ứng dụng web quản lý tài nguyên số (ảnh, link, bảng màu) với giao diện dark theme hiện đại, hỗ trợ kéo thả, tổ chức theo collection phân cấp, tag many-to-many, chia sẻ RBAC và real-time sync.

### Kiến trúc tổng thể

```
┌───────────────────────────────────────────────────────────────────┐
│                            CLIENTS                                │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐             │
│  │  React 19 SPA│  │  Mobile App  │  │  Public API  │             │
│  │  (Vite 7)    │  │  (future)    │  │  (future)    │             │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘             │
└─────────┼─────────────────┼─────────────────┼─────────────────────┘
          │ HTTP + WebSocket│                 │
          ▼                 ▼                 ▼
┌───────────────────────────────────────────────────────────────────┐
│                     REVERSE PROXY (Nginx)                         │
│  SPA fallback • Gzip • Cache /assets/ 1 year • Port 80            │
└───────────────────────────┬───────────────────────────────────────┘
                            │
                            ▼
┌───────────────────────────────────────────────────────────────────┐
│                   ASP.NET Core 9.0 Backend                        │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │ Middleware:                                                 │  │
│  │  ExceptionHandler → CORS → Serilog → RateLimiter →          │  │
│  │  StaticFiles → Auth → Controllers + SignalR Hub             │  │
│  ├─────────────────────────────────────────────────────────────┤  │
│  │ Controllers theo Feature + Domain (see Swagger):            │  │
│  │  AssetsCommand/Query • AssetLayout • BulkAssets •           │  │
│  │  Auth • Collections • Search • Tags • SmartCollections • ...│  │
│  ├─────────────────────────────────────────────────────────────┤  │
│  │ Services + Helpers (see Services folder):                                      │  │
│  │  Asset • BulkAsset • Collection • Auth • Storage •          │  │
│  │  Thumbnail • Tag • Notification • SmartCollection •         │  │
│  │  Permission • Search • AssetCleanupHelper                   │  │
│  ├─────────────────────────────────────────────────────────────┤  │
│  │ EF Core 9 (SQLite dev / PostgreSQL prod) • Entity tables (see EF migrations)         │  │
│  │ ASP.NET Identity • Auto-Migrate on Startup                  │  │
│  └─────────────────────────────────────────────────────────────┘  │
│  Port 5027 • JWT Bearer • SignalR (/hubs/assets)                  │
└──────────┬───────────────────┬──────────────────┬─────────────────┘
           │                   │                  │
           ▼                   ▼                  ▼
    ┌──────────────┐    ┌─────────────┐    ┌────────────────┐
    │ PostgreSQL 17│    │  Redis 7    │    │ Local Storage  │
    │ (Docker)     │    │  (Cache)    │    │ wwwroot/uploads│
    │ Port 5432    │    │  Port 6379  │    │ + /thumbs      │
    └──────────────┘    └─────────────┘    └────────────────┘
```

### Tech Stack

| Layer | Công nghệ | Phiên bản |
|-------|-----------|-----------|
| **Runtime** | .NET | 9.0 |
| **Web Framework** | ASP.NET Core | 9.0 |
| **ORM** | Entity Framework Core | 9.x |
| **DB (Dev)** | SQLite | 3.x (embedded) |
| **DB (Prod)** | PostgreSQL | 17 Alpine |
| **Cache** | Redis (StackExchangeRedis) | 7 Alpine |
| **Auth** | ASP.NET Identity + JWT Bearer | 9.x |
| **Real-time** | SignalR | 9.x |
| **Image Processing** | SixLabors.ImageSharp | 3.1.12 |
| **Logging** | Serilog (Console + File) | 10.x |
| **API Docs** | Swashbuckle/Swagger | 10.1.4 |
| **Frontend** | React | 19.2 |
| **Frontend Build** | Vite | 7.3.1 |
| **Routing** | react-router-dom | 7.13 |
| **HTTP Client** | axios | 1.13.5 |
| **File Upload** | react-dropzone | 15.0 |
| **SignalR Client** | @microsoft/signalr | 10.0 |
| **Container** | Docker + docker-compose | Multi-stage |

---

## 2. Kiến trúc Backend

### 2.1 Dependency Injection (Program.cs)

| Đăng ký | Lifetime | Mô tả |
|---------|----------|-------|
| CORS "Frontend" | Config | Origins từ appsettings, AllowCredentials cho SignalR |
| Rate Limiter "Fixed" | Config | 100 req/min, queue 10 |
| Rate Limiter "Upload" | Config | 20 req/min, queue 5 |
| Kestrel MaxRequestBodySize | Config | 100 MB |
| `AppDbContext` | Scoped | SQLite hoặc PostgreSQL theo `DatabaseProvider` config |
| `DatabaseProviderInfo` | Singleton | Record(ProviderName) với `IsPostgreSql` / `IsSqlite` |
| ASP.NET Identity | — | `ApplicationUser` + `IdentityRole` |
| JWT Bearer Auth | — | Validate Issuer/Audience/SigningKey, ClockSkew=Zero, SignalR query string support |
| Redis / MemoryCache | Singleton | Conditional: Redis nếu có connection string |
| `FileUploadConfig` | Singleton | 50MB max, 20 files/request, 27 extensions, 13 MIME prefixes |
| `IStorageService` → `LocalStorageService` | Scoped | Lưu file tại wwwroot/uploads |
| `IAssetService` → `AssetService` | Scoped | Asset business logic |
| `IBulkAssetService` → `BulkAssetService` | Scoped | Bulk delete/move/tag operations |
| `AssetCleanupHelper` | Scoped | File + thumbnail cleanup utility |
| `ICollectionService` → `CollectionService` | Scoped | Collection logic + Redis cache |
| `IAuthService` → `AuthService` | Scoped | Register/Login + JWT generation |
| `IThumbnailService` → `ThumbnailService` | Scoped | ImageSharp: sm/md/lg WebP |
| `ITagService` → `TagService` | Scoped | Tag CRUD + M2M |
| `INotificationService` → `NotificationService` | Scoped | SignalR notification wrapper |
| `ISmartCollectionService` → `SmartCollectionService` | Scoped | Dynamic virtual collections (Strategy pattern) |
| `ISmartCollectionFilter` → 5 strategies | Scoped | RecentDays, ContentType, Untagged, WithThumbnails, Tag |
| `IPermissionService` → `PermissionService` | Scoped | RBAC permission management |
| SignalR | — | Hub + group management |

### 2.2 Middleware Pipeline (thứ tự thực thi)

```
 Request ──►
 1. UseExceptionHandler()           ← Bắt mọi exception → ProblemDetails JSON
 2. UseCors("Frontend")             ← CORS cho SPA + SignalR
 3. UseSerilogRequestLogging()      ← HTTP request/response logging
 4. UseRateLimiter()                ← Fixed window rate limiting
 5. UseStaticFiles()                ← Serve uploads + thumbnails
 6. UseSwagger()                    ← Dev only
 7. UseAuthentication()             ← JWT Bearer validation
 8. UseAuthorization()              ← Authorization policies
 9. MapControllers()                ← REST API endpoints
10. MapHub<AssetHub>("/hubs/assets") ← SignalR WebSocket hub
 ◄── Response
```

### 2.3 Database Schema

**5 entity tables + ASP.NET Identity tables:**

| Table | Số cột | FK | Indexes |
|-------|--------|----|---------| 
| `Assets` | 18 | CollectionId, UserId, ParentFolderId(self) | 8 indexes |
| `Collections` | 10 | ParentId(self), UserId | 4 indexes |
| `Tags` | 6 | UserId | 3 indexes (incl. unique composite) |
| `AssetTags` | 2 | AssetId, TagId | Composite PK + 1 index |
| `CollectionPermissions` | 6 | UserId, CollectionId | Unique composite + 1 index |

**Tổng: indexes (see EF migrations)** — tối ưu cho common query patterns.

## Source of Truth

- API Endpoints: Swagger UI at /swagger
- Backend Structure: VAH.Backend/ folder
- Frontend Structure: VAH.Frontend/ folder
- Database Schema: EF Migrations in VAH.Backend/Migrations/ and AppDbContextModelSnapshot.cs
- Deployment: docker-compose.yml and Dockerfiles in respective folders

**Quan hệ giữa bảng:**

# PHẦN 2 — PHÂN TÍCH THIẾU SÓT & RỦI RO

## 2.1 Bảo mật (Security)

| Vấn đề | Mức rủi ro | Hiện trạng | Hậu quả nếu không cải thiện | Mitigation | DoD | Priority |
| --- | --- | --- | --- |
| **Authentication** | ✅ RESOLVED | JWT Bearer + ASP.NET Identity. `[Authorize]` trên tất cả endpoints (trừ Auth, Health) | Đã khắc phục | - | - | - |
| **Authorization** | ✅ RESOLVED | User-scoped data access. Mọi query filter theo UserId | Đã khắc phục | - | - | - |
| **User isolation** | ✅ RESOLVED | `UserId` FK trên Asset + Collection. System collections (null) shared, user data isolated | Đã khắc phục | - | - | - |
| **Data ownership** | ✅ RESOLVED | Service layer enforce `UserId == currentUser` trên mọi CRUD operation | Đã khắc phục | - | - | - |
| **Input validation** | ✅ IMPROVED | DTO validation đã được áp dụng ở nhiều endpoint (bao gồm upload/duplicate request models trong Assets feature) | Giảm rủi ro dữ liệu đầu vào không hợp lệ | - | - | - |
| **File upload protection** | ✅ RESOLVED | Có giới hạn size/type/số lượng qua `FileUploadConfig` + flow mapping `UploadedFileDto` | Giảm rủi ro DoS và upload file không hợp lệ | - | - | - |
| **XSS / Injection** | 🟡 MEDIUM | React auto-escape JSX, nhưng `dangerouslySetInnerHTML` potential qua link URL | URL độc hại (`javascript:`) có thể được lưu trong `FilePath` và render qua `<a href>` | Allow-list schemes (http/https), validate/sanitize on backend + safe rendering rules on frontend | All URLs validated, no raw href in JSX | P1 |
| **CORS policy** | ✅ RESOLVED | Config-driven origins (từ `appsettings`), `AllowCredentials` cho SignalR. Không phải `AllowAnyOrigin` | Giới hạn domain được phép gọi API | - | - | - |

## 2.2 Kiến trúc Backend

| Vấn đề | Mức rủi ro | Hiện trạng | Hậu quả |
| --- | --- | --- | --- |
| **Service layer** | ✅ RESOLVED | Interface-based DI, 12 services tách biệt (11 interface-backed + AssetCleanupHelper). BulkAssetService tách từ AssetService. SearchService extracted từ controller. Strategy pattern cho SmartCollection. Domain methods trên entities | Clean separation |
| **Controller SRP** | ✅ RESOLVED | Assets đã tách theo Command/Query + AssetLayout + BulkAssets; endpoint theo trách nhiệm rõ ràng | SRP compliant, dễ mở rộng theo vertical slice |
| **Repository pattern** | 🟡 MEDIUM | Service layer gọi `_context` trực tiếp (không qua Repository) | Coupling với EF Core, nhưng acceptable cho project size này |
| **Domain separation** | ✅ IMPROVED | Models có domain behavior, Enums tách riêng, DTOs gom vào Models/DTOs.cs | Rich Domain Model thay vì Anemic |
| **Exception handling** | ✅ RESOLVED | Global Exception Handler + ProblemDetails (RFC 7807) | Structured error responses |
| **Logging strategy** | ✅ RESOLVED | Serilog structured logging (Console + File sinks) | Full audit trail |
| **Pagination** | ✅ RESOLVED | `PagedResult<T>` + `PaginationParams` | Scalable data access |
| **Query optimization** | 🟡 MEDIUM | Indexes đã khai báo, nhưng N+1 risk vẫn có ở một số services | Cần review Include strategies |
| **Migration strategy** | ✅ RESOLVED | `Database.Migrate()` + EF Core Migrations | Schema version controlled |

## 2.3 Database & Dữ liệu

| Vấn đề | Mức rủi ro | Hiện trạng | Hậu quả |
| --- | --- | --- | --- |
| **Schema versioning** | ✅ RESOLVED | EF Core Migrations (5 migrations: InitialCreate, AddThumbnailColumns, AddTagSystem, AddCollectionPermissions, SyncModelChanges). Auto-migrate on startup | Schema version controlled | - | - | - |
| **Indexing** | ✅ RESOLVED | Composite indexes, FK indexes, UserId indexes đã khai báo trong AppDbContext | Optimized query performance | - | - | - |
| **Full-text search** | ✅ RESOLVED | Server-side search via SearchService (LIKE queries, paginated) | Search qua API, có pagination | - | - | - |
| **Tags system** | ✅ RESOLVED | Proper many-to-many (Tags table + AssetTags junction) với Tag entity, domain methods | Normalized, query-friendly | - | - | - |
| **Concurrency** | 🟡 MEDIUM | Không optimistic concurrency (no `RowVersion/ConcurrencyToken`) | 2 user sửa cùng asset → last write wins → data corruption âm thầm | Add RowVersion/ConcurrencyToken for key entities + return 409 on conflicts + client handling | Assets support concurrency control, conflicts return 409 | P1 |
| **FK constraints** | ✅ RESOLVED | Full FK constraints + navigation properties trong OnModelCreating. DeleteBehavior.Restrict cho self-ref | Referential integrity enforced | - | - | - |
| **SQLite limitations** | 🔴 HIGH (khi scale) | Single-writer lock, file-based, max recommend ~100 concurrent reads | Không hỗ trợ multi-server. Write contention khi >5 concurrent users |

## 2.4 Storage & Mở rộng

| Vấn đề | Mức rủi ro | Hiện trạng | Hậu quả |
| --- | --- | --- | --- |
| **File storage abstraction** | ✅ RESOLVED | `IStorageService` / `LocalStorageService`. Swap sang S3/Azure chỉ cần implement interface | Clean abstraction | - | - | - |
| **CDN** | 🟡 MEDIUM | Không có | Mọi request file đi qua backend server → bandwidth bottleneck | - | - | - |
| **Cloud storage readiness** | ✅ IMPROVED | IStorageService interface sẵn sàng cho cloud implementation | Chỉ cần thêm AzureBlobStorageService / S3StorageService | - | - | - |
| **Horizontal scaling** | 🔴 HIGH | SQLite file + local wwwroot | Không thể chạy 2+ instance. Sticky session bắt buộc → single point of failure | Explicitly require Postgres + shared storage; document SignalR scale strategy | Deployment docs include scaling requirements | P1 |
| **Thumbnail/preview** | ✅ RESOLVED | ThumbnailService sinh thumbnail + medium preview cho images | Bandwidth optimized | - | - | - |
| **Cleanup/orphan files** | ✅ RESOLVED | `asset.RequiresFileCleanup` virtual property, IStorageService.DeleteFile() gọi khi delete asset | No orphan files | - | - | - |

## 2.5 Frontend Architecture

| Vấn đề | Mức rủi ro | Hiện trạng | Hậu quả |
| --- | --- | --- | --- |
| **State management** | ✅ RESOLVED | `AppContext` + `AppProvider` (Context API) + `ConfirmContext` (dialog). `useAppContext()` hook. App.jsx 477 dòng (thêm TreeView, clipboard paste, folder multi-select). State centralised, không còn prop-drilling | Clean separation | - | - | - |
| **API abstraction** | ✅ RESOLVED | Class-based API layer: `BaseApiService` → 7 subclasses. `TokenManager` singleton. Barrel exports `api/index.js` | OOP-compliant, consistent, extensible | - | - | - |
| **Data fetching** | 🟡 MEDIUM | Manual `useEffect` + `useState` pattern | Không cache, dedup, background refetch, stale-while-revalidate. Re-fetch toàn bộ khi navigate | - | - | - |
| **Routing** | ✅ RESOLVED | React Router v7.13, URL sync qua useParams/useNavigate. 4 routes: /login, /, /collections/:id, /collections/:id/folder/:folderId | Shareable URLs, browser navigation works | - | - | - |
| **Code splitting** | 🟢 LOW | Không cần thiết hiện tại (8 components nhỏ) | Sẽ thành vấn đề khi bundle >500KB | - | - | - |
| **Error boundary** | ✅ RESOLVED | `ErrorBoundary.jsx` class component wrapping app | Graceful error fallback UI | - | - | - |
| **Loading/empty states** | 🟢 LOW | `loading` boolean nhưng chưa có skeleton UI | UX không mượt nhưng không phải risk kỹ thuật | - | - | - |

## 2.6 DevOps & Production Readiness

| Vấn đề | Mức rủi ro | Hiện trạng | Hậu quả |
| --- | --- | --- | --- |
| **Dockerization** | ✅ RESOLVED | Multi-stage Dockerfile + docker-compose (backend + postgres + redis + frontend) | Deploy reproducible | - | - | - |
| **Environment separation** | 🟡 MEDIUM | `API_URL` hardcode trong frontend. Swagger chỉ tắt theo env check | Không thể deploy staging/production mà không sửa source code | - | - | - | Remove hardcoded API_URL; standardize VITE_API_URL per environment | Frontend builds use environment-specific API_URL | P1 |
| **CI/CD** | 🟡 MEDIUM | Không có pipeline | Deploy thủ công → error-prone, chậm, không rollback | - | - | - |
| **Structured logging** | ✅ RESOLVED | Serilog + Console + File sinks, structured request logging | Full audit trail | - | - | - |
| **Monitoring** | ✅ RESOLVED | HealthController + `/api/v1/Health` endpoint, Docker healthchecks | System status visible | - | - | - |
| **Rate limiting** | ✅ RESOLVED | Fixed window 100 req/min + Upload 20 req/min | DoS protection | - | - | - |
| **Caching** | ✅ RESOLVED | Redis/In-memory distributed cache, CollectionService cached (TTL 5m) | 80%+ DB reads reduced | - | - | - |
| **HTTPS enforcement** | 🟡 MEDIUM | Default profile là HTTP | Data truyền plaintext → sniff được nội dung + các thông tin nhạy cảm | Redirect + HSTS in reverse proxy for production | Production deployments enforce HTTPS | P1 |

## 2.7 Testability

| Vấn đề | Mức rủi ro | Hiện trạng | Hậu quả |
| --- | --- | --- | --- |
| **Unit test** | 🟡 MEDIUM | Không có test project nào | Refactor = đánh cược. Regression bugs vào production |
| **Integration test** | 🟡 MEDIUM | Không có | API contract thay đổi không ai biết |
| **E2E test** | 🟢 LOW | Không có | Regression trên UI flow chỉ phát hiện bằng manual testing |
| **API contract testing** | 🟡 MEDIUM | Chỉ có Swagger cho dev | Frontend/Backend out of sync → runtime errors |

## 2.8 Đánh giá khả năng Scale hiện tại

```text
Concurrent Users:  Low concurrency (SQLite limitation)
Data Volume:       Large data volumes (pagination, indexes)
File Storage:      Significant storage (local disk, cleanup)
Deployment:        Single instance (Docker Compose) or multi-instance (PostgreSQL)
Availability:      Docker healthchecks, graceful degradation (Redis optional)
```
AspNetUsers ──┬──< Assets (UserId FK, Cascade)
              ├──< Collections (UserId FK, Cascade)
              ├──< Tags (UserId FK, Cascade)
              └──< CollectionPermissions (UserId FK, Cascade)

Collections ──┬──< Assets (CollectionId FK, Cascade)
              ├──< Collections (ParentId self-ref FK, SetNull)
              └──< CollectionPermissions (CollectionId FK, Cascade)

Assets ──< AssetTags ──> Tags  (M2M junction, Cascade both)
Assets ──< Assets (ParentFolderId self-ref, cho folders)
```

**Seed data:** 3 collections mặc định — Images (type=image), Links (type=link), Colors (type=color).

**Dual-provider:** `DatabaseProviderInfo` record cho phép AppDbContext tạo dialect-aware SQL (ví dụ: `datetime('now')` vs `now()` cho default values).

### 2.4 Exception Handling

`ExceptionHandlingMiddleware` map exception → HTTP status:

| Exception | Status | ProblemDetails |
|-----------|--------|----------------|
| `ArgumentException` | 400 | Bad Request |
| `KeyNotFoundException` | 404 | Not Found |
| `UnauthorizedAccessException` | 401 | Unauthorized |
| `InvalidOperationException` | 409 | Conflict |
| Tất cả khác | 500 | Internal Server Error |

Chi tiết exception chỉ hiện ở Development environment.

### 2.5 SignalR Hub

`AssetHub` tại `/hubs/assets`:

- **Auth:** `[Authorize]`, JWT via query string (`?access_token=...`)
- **Groups:** Mỗi user có group `user:{userId}`, join/leave tự động
- **10 event types:** AssetCreated, AssetUpdated, AssetDeleted, AssetsUploaded, AssetsBulkDeleted, AssetsBulkMoved, CollectionCreated, CollectionUpdated, CollectionDeleted, TagsChanged
- **NotificationService:** Gửi notification tới user group, silently catches errors

---

## 3. API Reference (đã đồng bộ route prefix `/api/v1`)

**Note:** API reference is synced with Swagger at `/swagger`. When routes change, update Swagger and this doc.

For backward-compat aliases, see notes below.

### 3.1 Assets — `api/v1/assets` [Authorize] (Query/Command/Layout + Bulk + domain-create)

| # | Method | Route | Status | Mô tả |
|---|--------|-------|--------|-------|
| 1 | GET | `/api/v1/assets` | 200 | Danh sách assets phân trang |
| 2 | GET | `/api/v1/assets/{id}` | 200/404 | Chi tiết asset |
| 3 | POST | `/api/v1/assets` | 201 | Tạo asset mới (CreateAssetDto) |
| 4 | POST | `/api/v1/assets/upload` | 201 | Upload multi-file |
| 5 | PATCH | `/api/v1/assets/{id}` | 200 | Cập nhật asset (partial) |
| 6 | PUT | `/api/v1/assets/{id}` | 200 | Cập nhật asset (backward compat alias) |
| 7 | PUT | `/api/v1/assets/{id}/position` | 200 | Cập nhật vị trí trên canvas |
| 8 | DELETE | `/api/v1/assets/{id}` | 204 | Xóa asset + file vật lý + thumbnails |
| 9 | POST | `/api/v1/assets/{id}/duplicate` | 201 | Duplicate asset |
| 10 | POST | `/api/v1/assets/reorder` | 204 | Sắp xếp lại thứ tự |
| 11 | GET | `/api/v1/assets/group/{groupId}` | 200 | Assets theo nhóm |
| 12 | POST | `/api/v1/assets/folders` | 201 | Tạo thư mục |
| 13 | POST | `/api/v1/assets/colors` | 201 | Tạo asset màu sắc |
| 14 | POST | `/api/v1/assets/color-groups` | 201 | Tạo nhóm màu |
| 15 | POST | `/api/v1/assets/links` | 201 | Tạo liên kết |

**BulkAssetsController** — `api/v1/assets` [Authorize] (4 endpoints)

| # | Method | Route | Status | Mô tả |
|---|--------|-------|--------|-------|
| 1 | POST | `/api/v1/assets/bulk-delete` | 200 | Xóa hàng loạt |
| 2 | POST | `/api/v1/assets/bulk-move` | 200 | Di chuyển hàng loạt |
| 3 | POST | `/api/v1/assets/bulk-move-group` | 200 | Di chuyển màu giữa các group với vị trí chính xác |
| 4 | POST | `/api/v1/assets/bulk-tag` | 200 | Gắn/gỡ tag hàng loạt |

### 3.2 Auth — `api/v1/Auth` [RateLimited]

| # | Method | Route | Mô tả |
|---|--------|-------|-------|
| 1 | POST | `/api/v1/Auth/register` | Đăng ký → JWT token + user info |
| 2 | POST | `/api/v1/Auth/login` | Đăng nhập → JWT token + user info |

### 3.3 Collections — `api/v1/Collections` [Authorize]

| # | Method | Route | Mô tả |
|---|--------|-------|-------|
| 1 | GET | `/api/v1/Collections` | Tất cả collections (own + system + shared) |
| 2 | GET | `/api/v1/Collections/{id}/items` | Items + sub-collections (permission-aware) |
| 3 | POST | `/api/v1/Collections` | Tạo collection mới |
| 4 | PATCH | `/api/v1/Collections/{id}` | Cập nhật partial |
| 5 | PUT | `/api/v1/Collections/{id}` | Cập nhật (backward compat alias) |
| 6 | DELETE | `/api/v1/Collections/{id}` | Xóa (owner only) |

### 3.4 Search — `api/v1/Search` [Authorize]

| # | Method | Route | Mô tả |
|---|--------|-------|-------|
| 1 | GET | `/api/v1/Search?q=&type=&collectionId=&page=&pageSize=` | Tìm kiếm assets + collections |

### 3.5 Tags — `api/v1/Tags` [Authorize]

| # | Method | Route | Mô tả |
|---|--------|-------|-------|
| 1 | GET | `/api/v1/Tags` | Tất cả tags của user |
| 2 | GET | `/api/v1/Tags/{id}` | Chi tiết tag |
| 3 | POST | `/api/v1/Tags` | Tạo tag (dedup normalized) |
| 4 | PUT | `/api/v1/Tags/{id}` | Cập nhật tag |
| 5 | DELETE | `/api/v1/Tags/{id}` | Xóa tag |
| 6 | GET | `/api/v1/Tags/asset/{assetId}` | Tags của 1 asset |
| 7 | PUT | `/api/v1/Tags/asset/{assetId}` | Set toàn bộ tags (replace) |
| 8 | POST | `/api/v1/Tags/asset/{assetId}/add` | Thêm tags |
| 9 | POST | `/api/v1/Tags/asset/{assetId}/remove` | Gỡ tags |
| 10 | POST | `/api/v1/Tags/migrate` | Migrate comma-separated → M2M |

### 3.6 Smart Collections — `api/v1/SmartCollections` [Authorize]

| # | Method | Route | Mô tả |
|---|--------|-------|-------|
| 1 | GET | `/api/v1/SmartCollections` | Danh sách smart collections (8 built-in + per-tag) |
| 2 | GET | `/api/v1/SmartCollections/{id}/items` | Items phân trang |

### 3.7 Permissions — `api/v1/collections/{collectionId}/permissions` [Authorize]

| # | Method | Route | Mô tả |
|---|--------|-------|-------|
| 1 | GET | `.../permissions` | Danh sách quyền (viewer+ access) |
| 2 | POST | `.../permissions` | Cấp quyền (owner only, by email) |
| 3 | PUT | `.../permissions/{permissionId}` | Cập nhật role (owner only) |
| 4 | DELETE | `.../permissions/{permissionId}` | Thu hồi (owner only) |
| 5 | GET | `.../permissions/my-role` | Role hiện tại |
| 6 | GET | `/api/v1/shared-collections` | Collections được chia sẻ cho user |

### 3.8 Health — `api/v1/Health` [No Auth]

| # | Method | Route | Mô tả |
|---|--------|-------|-------|
| 1 | GET | `/api/v1/Health` | DB + Storage checks, env info (200/503) |

### Auth Rules

| Endpoint Type | Auth Required | Notes |
|---------------|---------------|-------|
| Auth (/api/v1/Auth/*) | No | Rate limited |
| Health (/api/v1/Health) | No | Public health check |
| All others | Yes | JWT Bearer |

---

## 4. Kiến trúc Frontend

### 4.1 Cấu trúc thư mục

```
VAH.Frontend/src/
├── main.jsx                     # StrictMode → ErrorBoundary → BrowserRouter → AuthProvider → App
├── App.jsx                      # AppLayout + Routes (344 lines, refactored từ 620)
├── App.css                      # Dark Navy theme (24 CSS variables)
│
├── api/                         # 11 API files (class-based)
│   ├── BaseApiService.js        # Abstract base class (_get/_post/_put/_delete)
│   ├── TokenManager.js          # JWT token singleton (private #storageKey)
│   ├── client.js                # Axios instance + JWT interceptor + staticUrl
│   ├── assetsApi.js             # AssetApiService (16 methods)
│   ├── authApi.js               # AuthApiService
│   ├── collectionsApi.js        # CollectionApiService
│   ├── searchApi.js             # SearchApiService
│   ├── tagsApi.js               # TagApiService (10 methods)
│   ├── smartCollectionsApi.js   # SmartCollectionApiService
│   ├── permissionsApi.js        # PermissionApiService (6 methods)
│   └── index.js                 # Barrel file — re-exports all singletons
│
├── context/                     # State management (Context API)
│   ├── AppContext.js            # AppProvider + useAppContext() — centralised state (387 dòng)
│   └── ConfirmContext.js        # ConfirmProvider + useConfirm() — promise-based confirm/prompt/alert (121 dòng)
│
├── models/                      # Domain model classes
│   └── index.js                 # Asset, Collection, Tag classes + mapping helpers
│
├── hooks/                       # 11 custom hooks
│   ├── useAuth.js               # AuthProvider context + login/register/logout
│   ├── useAssets.js             # CRUD + compose useAssetSelection + useBulkOperations
│   ├── useAssetSelection.js     # Multi-select state: toggle, range, selectAll
│   ├── useBulkOperations.js     # Bulk delete/move/tag/moveGroup
│   ├── useCollections.js        # State + CRUD + compose useCollectionNavigation
│   ├── useCollectionNavigation.js # URL sync, breadcrumbs, folder path
│   ├── useSmartCollections.js   # Smart collection fetch + state
│   ├── useSharePermissions.js   # Permission CRUD (grant/updateRole/revoke)
│   ├── useTags.js               # Tag CRUD + asset-tag M2M
│   ├── useSignalR.js            # Real-time connection + events
│   └── useUndoRedo.js           # Command pattern (50 history)
│
└── components/                  # 17 components
    ├── AppHeader.jsx            # Header bar (search, actions, notifications)
    ├── AppSidebar.jsx           # Sidebar (collection tree, smart collections, pinned items)
    ├── DetailsPanel.jsx         # Right panel (preview, metadata, tags)
    ├── LoginPage.jsx            # Login/Register form
    ├── ErrorBoundary.jsx        # React error boundary (class component)
    ├── CollectionTree.jsx       # Sidebar tree navigation
    ├── CollectionBrowser.jsx    # File browser (grid/list/masonry, context menu)
    ├── AssetDisplayer.jsx       # Asset gallery + canvas
    ├── AssetGrid.jsx            # Simple card grid
    ├── UploadArea.jsx           # Drag-and-drop upload zone
    ├── ColorBoard.jsx           # Color palette manager (drag-drop, multi-select, context menu) (555 dòng)
    ├── SearchBar.jsx            # Search input
    ├── ShareDialog.jsx          # RBAC sharing dialog (presentational, logic in useSharePermissions)
    ├── DraggableAssetCanvas.jsx # Canvas drag-and-drop
    ├── ContextMenu.jsx          # Reusable right-click context menu (81 dòng)
    ├── ConfirmDialog.jsx        # Unified styled confirm/prompt/alert dialog (133 dòng)
    └── TreeViewPanel.jsx        # Right sidebar tree view — hierarchical structure (489 dòng)
```

### 4.2 Routing

| Path | Component | Mô tả |
|------|-----------|-------|
| `/login` | LoginPage | Redirect → `/` nếu đã đăng nhập |
| `/` | AppLayout | Trang chủ hoặc collection browser |
| `/collections/:collectionId` | AppLayout | Xem collection |
| `/collections/:collectionId/folder/:folderId` | AppLayout | Xem thư mục con |
| `*` | Redirect → `/` | Fallback |

### 4.3 State Management

Không sử dụng Redux/Zustand — hoàn toàn React hooks + Context API:

| Hook | Chức năng | Đặc điểm |
|------|-----------|----------|
| `useAuth()` | Auth state, login/register/logout | Context Provider, persist localStorage |
| `useAppContext()` | Centralised app state | Context API, compose tất cả domain hooks |
| `useCollections()` | Collection list, CRUD | Compose `useCollectionNavigation` |
| `useCollectionNavigation()` | URL sync, breadcrumbs, folder path | useParams/useNavigate |
| `useAssets()` | Asset CRUD | Compose `useAssetSelection` + `useBulkOperations` |
| `useAssetSelection()` | Multi-select state | Ctrl+click toggle, Shift+click range |
| `useBulkOperations()` | Bulk delete/move/tag/moveGroup | Batch API calls |
| `useSmartCollections()` | Smart collection fetch + state | Auto-fetch, virtual collections |
| `useSharePermissions()` | Permission CRUD | grant, updateRole, revoke |
| `useTags()` | Tag CRUD, asset-tag management | Auto-fetch on mount |
| `useSignalR()` | Real-time events | Auto-reconnect [0, 2s, 5s, 10s, 30s] |
| `useUndoRedo()` | Command pattern undo/redo | Ctrl+Z / Ctrl+Shift+Z, max 50 history |

### 4.4 Layout

```
┌────────────────────────────────────────────────────────────────┐
│  HEADER (56px)                                                  │
│  Logo • Search Input • Folder/Settings/Notifications/Logout    │
├──────────┬───────────────────────────────────────┬─────────────┤
│ SIDEBAR  │              MAIN AREA                 │  DETAILS    │
│ (240px)  │                                        │  (320px)    │
│          │  Toolbar: Breadcrumbs + Actions + View │             │
│ Tài liệu │  ┌──────────────────────────────────┐  │  Preview    │
│ của tôi  │  │                                    │  │  Metadata   │
│ ├ Images │  │  CollectionBrowser (grid/list/     │  │  Tags       │
│ ├ Links  │  │  masonry) hoặc ColorBoard          │  │  Tag mgmt   │
│ ├ Colors │  │  hoặc SmartCollection view         │  │             │
│ └ Custom │  │                                    │  │             │
│          │  └──────────────────────────────────┘  │             │
│ Smart    │                                        │             │
│ Collections│  Upload Section (react-dropzone)     │             │
│ ├ Gần đây│  Bulk Actions Bar (multi-select)      │             │
│ ├ Images │                                        │             │
│ └ ...    │                                        │             │
├──────────┴───────────────────────────────────────┴─────────────┤
│  ShareDialog (modal overlay) — hiện khi nhấn "Chia sẻ"         │
└────────────────────────────────────────────────────────────────┘
```

---

## 5. Docker & Deployment

### 5.1 Docker Compose — 4 Services

| Service | Image | Port | Depends On | Healthcheck |
|---------|-------|------|------------|-------------|
| `postgres` | postgres:17-alpine | 5432 | — | `pg_isready` |
| `redis` | redis:7-alpine | 6379 | — | `redis-cli ping` |
| `backend` | Build `./VAH.Backend` | 5027 | postgres, redis | `wget /api/v1/Health` |
| `frontend` | Build `./VAH.Frontend` | 3000→80 | backend | — |

### Supported Deployment Modes

| Mode | Description | Expected Concurrency | Storage Requirements | SignalR Approach |
|------|-------------|----------------------|----------------------|------------------|
| A | SQLite + local uploads + single instance (dev/homelab) | ~5-10 concurrent users | ~5-10GB local disk | N/A (single instance) |
| B | PostgreSQL + local uploads + single instance (small prod) | ~50-100 concurrent users | ~100GB+ local disk | N/A (single instance) |
| C | PostgreSQL + object storage + multi-instance (scale) | 100+ concurrent users | Object storage (S3/Azure) | Redis backplane + sticky sessions |

For each mode, SignalR is handled via user groups; multi-instance requires Redis backplane to broadcast messages across instances.

### 5.2 Named Volumes

| Volume | Mô tả |
|--------|-------|
| `postgres-data` | PostgreSQL data persistence |
| `redis-data` | Redis data persistence |
| `backend-uploads` | User uploaded files |
| `backend-logs` | Serilog log files |

### 5.3 Docker Build Details

**Backend Dockerfile:**
- Multi-stage: SDK 9.0 (build) → ASP.NET 9.0 (runtime)
- Non-root user `appuser:appgroup`
- Auto-creates `/app/wwwroot/uploads`, `/app/wwwroot/uploads/thumbs`, `/app/logs`

**Frontend Dockerfile:**
- Multi-stage: Node 22 Alpine (build) → Nginx Alpine (serve)
- Build arg: `VITE_API_URL` cho backend URL
- Custom nginx.conf: SPA fallback, gzip, 1-year cache `/assets/`

---

## 6. Runbook

### Local Run Instructions

Prerequisites: .NET 9 SDK, Node.js 22, optionally Docker for DB.

1. Clone the repo.

2. For backend: cd VAH.Backend, dotnet run

3. For frontend: cd VAH.Frontend, npm install, npm run dev

Ports: Backend 5027, Frontend 3000

### Docker Compose Run

1. docker-compose up

Ports: Backend 5027, Frontend 80

### Database Migration

Auto-migrate on startup. For production, review migrations before deploy.

### Backup/Restore

- PostgreSQL: pg_dump / pg_restore

- Uploads: tar the volume

### Secrets

- JWT key: Rotate via appsettings, restart

- DB connection: Use environment variables

## 7. Bảo mật

| Biện pháp | Chi tiết |
|-----------|---------|
| **JWT Authentication** | 24h expiration, ClockSkew=Zero, HS256 |
| **Password Policy** | Min 6 chars, require digit + lowercase |
| **Data Ownership** | UserId FK trên mọi entity, user chỉ truy cập dữ liệu của mình |
| **RBAC Permissions** | 3 roles (Owner/Editor/Viewer) trên collection, check ở service layer |
| **CORS** | Config-driven origins, AllowCredentials cho SignalR |
| **Rate Limiting** | Fixed window: 100 req/min (general), 20 req/min (upload) |
| **File Validation** | Size (50MB max), extension whitelist (27 types), MIME type check |
| **Exception Privacy** | Exception details chỉ ở Development mode |
| **SignalR Auth** | JWT via query string, user-scoped groups |
| **Docker Security** | Non-root container user, isolated volumes |

---

## 8. Hiệu suất

| Strategy | Chi tiết |
|----------|---------|
| **Redis Cache** | Collection list: 5 min absolute / 2 min sliding |
| **Fallback Cache** | In-memory khi không có Redis |
| **Cache Invalidation** | Auto-invalidate sau mutation |
| **Thumbnail Pre-gen** | sm(150px)/md(400px)/lg(800px) WebP, quality 80 |
| **Nginx Cache** | /assets/ 1-year immutable |
| **Gzip** | css/js/json/svg |
| **DB Indexes** | 22 indexes cho common queries |
| **Pagination** | Server-side, max 100 items/page |

---

## 9. Lộ trình Phát triển — Tổng kết 4 Giai đoạn

### Giai đoạn 1 — Nền tảng Production ✅ 7/7 (100%)

| # | Hạng mục | Files liên quan |
|---|----------|-----------------|
| 1 | Authentication (JWT + Identity) | AuthService, AuthController, useAuth |
| 2 | User Entity + Data Ownership | ApplicationUser, UserId FK |
| 3 | EF Core Migrations | 4 migrations (Initial, Thumbnails, Tags, Permissions) |
| 4 | Exception Handling | ExceptionHandlingMiddleware |
| 5 | Validation | FileUploadConfig, DTO annotations |
| 6 | File Upload Restrictions | AssetService.UploadFilesAsync |
| 7 | Pagination | PagedResult\<T\>, PaginationParams |

### Giai đoạn 2 — Architecture ✅ 6/6 (100%)

| # | Hạng mục | Files liên quan |
|---|----------|-----------------|
| 1 | Service Layer | 9 services với interfaces |
| 2 | Server-side Search | SearchController, SearchResult DTO |
| 3 | Database Indexing | 22 indexes trong AppDbContext |
| 4 | Storage Abstraction | IStorageService → LocalStorageService |
| 5 | Frontend State Refactor | 6 custom hooks |
| 6 | React Router | useCollections URL sync, 4 routes |

### Giai đoạn 3 — Production-Grade ✅ 7/7 (100%)

| # | Hạng mục | Files liên quan |
|---|----------|-----------------|
| 1 | PostgreSQL Dual-Provider | DatabaseProviderInfo, dialect-aware SQL |
| 2 | Docker + docker-compose | 4 services, multi-stage, healthchecks |
| 3 | Redis Cache | CollectionService cache, 5min/2min TTL |
| 4 | Serilog Logging | Console + File sinks, 30-day retention |
| 5 | Health Check | HealthController: DB + Storage |
| 6 | Rate Limiting | Fixed window: 100/min, 20/min upload |
| 7 | Thumbnails | ThumbnailService: sm/md/lg WebP |

### Giai đoạn 4 — Nâng cấp Sản phẩm ✅ 6/6 (100%)

| # | Hạng mục | Files liên quan |
|---|----------|-----------------|
| 1 | Smart Collections | SmartCollectionService, 8 built-in + per-tag |
| 2 | Tag System (M2M) | Tag, AssetTag, TagService, TagsController |
| 3 | Bulk Operations | bulk-delete/move/tag, Ctrl/Shift multi-select |
| 4 | Undo/Redo | useUndoRedo hook, Ctrl+Z/Ctrl+Shift+Z |
| 5 | Real-time Sync | AssetHub, NotificationService, useSignalR |
| 6 | RBAC Permissions | CollectionPermission, PermissionService, ShareDialog |

---

## 10. Tổng kết Metrics

| Metric | Giá trị |
|--------|---------|
| Tổng API endpoints | See Swagger for current count |
| Backend controllers | See VAH.Backend/Controllers/ folder |
| Backend services | See VAH.Backend/Services/ folder |
| Frontend hooks | See VAH.Frontend/src/hooks/ folder |
| Frontend components | See VAH.Frontend/src/components/ folder |
| Frontend context | See VAH.Frontend/src/context/ folder |
| Frontend models | See VAH.Frontend/src/models/ folder |
| API modules (frontend) | See VAH.Frontend/src/api/ folder |
| Database tables | See EF migrations and AppDbContextModelSnapshot |
| Database indexes | See EF migrations and AppDbContextModelSnapshot |
| Docker services | See docker-compose.yml |
| EF Migrations | See VAH.Backend/Migrations/ folder |
| Giai đoạn hoàn thành | **4/4 (26/26 — 100%) + OOP refactor 5/5 Phases (23/23) + Session #4-6** |

## 11. Glossary

- **Asset:** A digital resource (image, link, color) managed in the system.
- **Collection:** Hierarchical folder for organizing assets.
- **Smart Collection:** Dynamic virtual collection based on filters (e.g., recent, untagged).
- **RBAC Roles:** Owner (full access), Editor (read/write), Viewer (read-only) for collections.
- **Feature Slice:** Architectural pattern separating Command, Query, and Application layers.
- **Command/Query Separation:** CQRS-inspired split for write (Command) and read (Query) operations.
- **System Collections:** Default collections (Images, Links, Colors) shared across users.
- **Tag:** Many-to-many labels for assets.
- **JWT Bearer:** JSON Web Token authentication.
- **SignalR:** Real-time WebSocket communication.
- **EF Core:** Entity Framework Core ORM.
- **Docker Compose:** Multi-container deployment.

## 12. Documentation Maintenance

- **API Changes:** When routes or schemas change, update Swagger UI and sync this API reference section.
- **Code Changes:** When adding services, controllers, entities, or migrations, update references to source of truth (e.g., folder links) instead of hardcoding counts.
- **Versioning:** Ensure "Last updated" date and version match the current release or tag.
- **Review Cycle:** Review for stale numbers and outdated information quarterly or before major releases.

---

> *Tài liệu này đã được đồng bộ tối thiểu theo trạng thái hiện tại: route prefix `/api/v1`, Assets theo Feature Slice (Command/Query/Application/Infrastructure), và wiring qua DI/Strategy/Options. Các phần roadmap lịch sử được giữ nguyên để tham chiếu tiến trình dự án.*
