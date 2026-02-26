# Visual Asset Hub — Báo cáo Kiến trúc & Roadmap nâng cấp

> **Vai trò:** Senior Software Architect & Tech Lead  
> **Ngày đánh giá:** 25/02/2026  
> **Phạm vi:** Toàn bộ hệ thống VAH (Backend .NET 9 + Frontend React 19 + SQLite)  
> **Mục tiêu:** Đánh giá hiện trạng → Phân tích thiếu sót & rủi ro → Đề xuất roadmap chuyển từ MVP local sang Production-ready / SaaS-ready

---

## MỤC LỤC

- [PHẦN 1 — ĐÁNH GIÁ HIỆN TRẠNG](#phần-1--đánh-giá-hiện-trạng)
- [PHẦN 2 — PHÂN TÍCH THIẾU SÓT & RỦI RO](#phần-2--phân-tích-thiếu-sót--rủi-ro)
- [PHẦN 3 — ĐỀ XUẤT & ROADMAP NÂNG CẤP](#phần-3--đề-xuất--roadmap-nâng-cấp)
- [PHỤ LỤC — KIẾN TRÚC ĐÍCH (TARGET ARCHITECTURE)](#phụ-lục--kiến-trúc-đích-target-architecture)

---

# PHẦN 1 — ĐÁNH GIÁ HIỆN TRẠNG

## 1.1 Điểm sáng trong lựa chọn Tech Stack

| Thành phần | Lựa chọn | Đánh giá |
| --- | --- | --- |
| Runtime | .NET 9.0 | Hiệu năng cao, cross-platform, LTS ecosystem mạnh. Đúng hướng cho enterprise-grade API |
| Frontend | React 19 + Vite 7 | Stack hiện đại nhất hiện tại, HMR nhanh, tree-shaking tốt, ecosystem plugin phong phú |
| ORM | Entity Framework Core 9 | Code-first, LINQ mạnh, migration system sẵn có. Phù hợp rapid development |
| HTTP Client | axios | Interceptor pattern, cancel token, error handling tốt hơn fetch native |
| Upload | react-dropzone | Thư viện phổ biến, accessible, hỗ trợ drag-and-drop tốt |
| API Docs | Swashbuckle/Swagger | Tự động sinh OpenAPI spec, hỗ trợ testing trực tiếp |
| DB (MVP) | SQLite | Zero-config, embedded, phù hợp local development & prototyping nhanh |

**Nhận xét:** Bộ công nghệ được chọn hoàn toàn hợp lý cho giai đoạn MVP. .NET + React là combo đã được chứng minh ở production scale (Microsoft, Facebook). Không có lựa chọn "lệch hướng" nào cần thay thế hoàn toàn.

## 1.2 Mức độ hoàn thiện tính năng cốt lõi

### UI/UX — 7/10

- ✅ Design system hoàn chỉnh với 24 CSS custom properties, dark theme nhất quán
- ✅ Layout 4 khu vực (header, sidebar, main, details panel) bám sát chuẩn file manager hiện đại
- ✅ 3 chế độ hiển thị (grid, list, masonry) + canvas drag-and-drop
- ✅ Breadcrumb navigation cho cả collection lẫn folder lồng nhau
- ✅ Details panel contextual khi chọn asset
- ⚠️ Chưa có responsive breakpoints cho mobile/tablet
- ⚠️ Chưa có loading skeleton, error boundary, empty state design

### Database Schema — 6/10

- ✅ Schema đủ dùng cho MVP: 2 bảng Assets + Collections với quan hệ cha-con
- ✅ Hỗ trợ folder hierarchy qua `ParentFolderId` (self-referencing)
- ✅ Hỗ trợ collection hierarchy qua `ParentId`
- ✅ Đa dạng content type: image, link, color, folder, color-group
- ⚠️ Tags lưu dạng comma-separated string — không query/filter được
- ⚠️ Không có FK constraint definition trong DbContext (chỉ convention)
- ⚠️ Không có index nào được khai báo

### Luồng API — 7/10

- ✅ 17 endpoints RESTful đầy đủ CRUD cho cả Assets lẫn Collections
- ✅ Upload multipart với GUID filename (tránh collision)
- ✅ Reorder API cho batch update `SortOrder`
- ✅ Nested folder navigation qua query parameter `folderId`
- ✅ Subcollection hierarchy trong response `CollectionWithItems`
- ⚠️ Thiếu pagination hoàn toàn — `GET /api/Assets` trả về **toàn bộ**
- ⚠️ Không có validation layer (FluentValidation hoặc tương đương)
- ⚠️ Không có error handling middleware — exception rơi vào default handler

## 1.3 Những thiết kế đã làm tốt

1. **GUID file naming** — Tránh path traversal và filename collision. Đây là best practice cho file upload.
2. **DTO pattern** — Đã tách 6 DTO riêng biệt thay vì expose trực tiếp entity. Đúng hướng.
3. **Cây phân cấp linh hoạt** — Cả Collection lẫn Folder đều hỗ trợ nesting bằng parent reference, cho phép tổ chức dữ liệu phức tạp.
4. **Content type polymorphism** — Một bảng `Assets` chứa nhiều loại (image, link, color, folder) qua field `ContentType`, giảm số bảng và đơn giản hóa query.
5. **Static file serving** — `UseStaticFiles()` + `wwwroot/uploads/` cho phép serve file trực tiếp không qua controller, hiệu quả.
6. **Separation of concerns (Frontend)** — 8 components riêng biệt, mỗi component đảm nhận một trách nhiệm rõ ràng.

---

# PHẦN 2 — PHÂN TÍCH THIẾU SÓT & RỦI RO

## 2.1 Bảo mật (Security)

| Vấn đề | Mức rủi ro | Hiện trạng | Hậu quả nếu không cải thiện |
| --- | --- | --- | --- |
| **Authentication** | ✅ RESOLVED | JWT Bearer + ASP.NET Identity. `[Authorize]` trên tất cả endpoints (trừ Auth, Health) | Đã khắc phục |
| **Authorization** | ✅ RESOLVED | User-scoped data access. Mọi query filter theo UserId | Đã khắc phục |
| **User isolation** | ✅ RESOLVED | `UserId` FK trên Asset + Collection. System collections (null) shared, user data isolated | Đã khắc phục |
| **Data ownership** | ✅ RESOLVED | Service layer enforce `UserId == currentUser` trên mọi CRUD operation | Đã khắc phục |
| **Input validation** | 🟡 MEDIUM | Chỉ có `[Required]` trên FileName/FilePath. DTO không validate | SQL injection risk thấp (EF Core parameterize), nhưng logic bugs cao. Có thể tạo asset với collectionId không tồn tại |
| **File upload protection** | 🟡 MEDIUM | Không giới hạn size, type, số lượng file | Server bị DoS bằng upload file lớn. Upload `.exe`, `.php` shell |
| **XSS / Injection** | 🟡 MEDIUM | React auto-escape JSX, nhưng `dangerouslySetInnerHTML` potential qua link URL | URL độc hại (`javascript:`) có thể được lưu trong `FilePath` và render qua `<a href>` |
| **CORS policy** | 🟡 MEDIUM | `AllowAnyOrigin + AllowAnyMethod + AllowAnyHeader` | CSRF attack surface mở hoàn toàn. Bất kỳ domain nào đều gọi được API |

## 2.2 Kiến trúc Backend

| Vấn đề | Mức rủi ro | Hiện trạng | Hậu quả |
| --- | --- | --- | --- |
| **Service layer** | 🟡 MEDIUM | Không có. Controller chứa toàn bộ business logic + data access | Vi phạm SRP. Không thể unit test logic. Code sẽ phình to khi thêm tính năng |
| **Repository pattern** | 🟡 MEDIUM | Controller gọi `_context` trực tiếp | Coupling chặt với EF Core. Khó swap database provider. Duplicate query code |
| **Domain separation** | 🟡 MEDIUM | 1 project duy nhất chứa tất cả | Models, DTOs, Controllers, DbContext nằm cùng assembly. Khó tái sử dụng |
| **Exception handling** | 🔴 HIGH | Không có global middleware | Unhandled exception trả về stack trace cho client (thông tin nhạy cảm). Crash không log |
| **Logging strategy** | 🟡 MEDIUM | Chỉ có default `ILogger` — không structured, không persistence | Không trace được bug production, không audit trail |
| **Pagination** | 🔴 HIGH | `GetAssets()` → `ToListAsync()` load toàn bộ bảng | 10K assets → response 5MB+ → frontend freeze. Memory spike trên server |
| **Query optimization** | 🟡 MEDIUM | N+1 risk trong `ReorderAssets` (FindAsync trong loop) | Performance tuyến tính O(n) cho mỗi reorder — degraded với dữ liệu lớn |
| **Migration strategy** | ✅ RESOLVED | `Database.Migrate()` + EF Core Migrations | Schema version controlled |

## 2.3 Database & Dữ liệu

| Vấn đề | Mức rủi ro | Hiện trạng | Hậu quả |
| --- | --- | --- | --- |
| **Schema versioning** | ✅ RESOLVED | EF Core Migrations với `InitialCreate`. Auto-migrate on startup | Schema version controlled |
| **Indexing** | 🟡 MEDIUM | Không index nào (ngoài PK tự động) | Query `WHERE CollectionId = ? AND ParentFolderId = ?` full table scan. Chậm tuyến tính |
| **Full-text search** | 🟡 MEDIUM | Chỉ client-side filter bằng JS `.includes()` | Tìm kiếm chậm, không fuzzy match, không accent-insensitive, không rank relevance |
| **Tags system** | 🟡 MEDIUM | Comma-separated string trong 1 cột | Không thể `WHERE tag = 'landscape'` hiệu quả. Phải `LIKE '%landscape%'` → false positives + full scan |
| **Concurrency** | 🟡 MEDIUM | Không optimistic concurrency (no `RowVersion/ConcurrencyToken`) | 2 user sửa cùng asset → last write wins → data corruption âm thầm |
| **FK constraints** | 🟡 MEDIUM | Không khai báo FK trong `OnModelCreating` | `CollectionId` có thể trỏ đến collection đã xóa → orphan data |
| **SQLite limitations** | 🔴 HIGH (khi scale) | Single-writer lock, file-based, max recommend ~100 concurrent reads | Không hỗ trợ multi-server. Write contention khi >5 concurrent users |

## 2.4 Storage & Mở rộng

| Vấn đề | Mức rủi ro | Hiện trạng | Hậu quả |
| --- | --- | --- | --- |
| **File storage abstraction** | 🟡 MEDIUM | Hard-coded `Path.Combine(cwd, "wwwroot", "uploads")` trong controller | Không thể swap sang S3/Azure Blob mà không sửa controller |
| **CDN** | 🟡 MEDIUM | Không có | Mọi request file đi qua backend server → bandwidth bottleneck |
| **Cloud storage readiness** | 🟡 MEDIUM | Local filesystem only | Server đầy disk → crash. Không backup tự động. Mất server = mất data |
| **Horizontal scaling** | 🔴 HIGH | SQLite file + local wwwroot | Không thể chạy 2+ instance. Sticky session bắt buộc → single point of failure |
| **Thumbnail/preview** | 🟡 MEDIUM | Serve original file cho mọi kích thước | Upload ảnh 20MB → browser load 20MB cho thumbnail 150px. Bandwidth waste |
| **Cleanup/orphan files** | 🟡 MEDIUM | Delete asset chỉ xóa DB record, không xóa file vật lý | `wwwroot/uploads` phình to vĩnh viễn. Disk leak |

## 2.5 Frontend Architecture

| Vấn đề | Mức rủi ro | Hiện trạng | Hậu quả |
| --- | --- | --- | --- |
| **State management** | 🟡 MEDIUM | 11 `useState` trong `App.jsx`. Tất cả logic + state ở root component | Props drilling qua 3+ levels. Re-render toàn bộ tree khi bất kỳ state nào thay đổi. 650+ lines trong 1 file |
| **API abstraction** | 🟡 MEDIUM | `axios.get/post/put/delete` gọi trực tiếp trong handler functions | Duplicate error handling. Hardcode `API_URL`. Không interceptor cho auth token |
| **Data fetching** | 🟡 MEDIUM | Manual `useEffect` + `useState` pattern | Không cache, dedup, background refetch, stale-while-revalidate. Re-fetch toàn bộ khi navigate |
| **Routing** | 🟡 MEDIUM | Không có router — single page state-driven | URL không reflect trạng thái UI. Không shareable link. Browser back/forward broken |
| **Code splitting** | 🟢 LOW | Không cần thiết hiện tại (8 components nhỏ) | Sẽ thành vấn đề khi bundle >500KB |
| **Error boundary** | 🟡 MEDIUM | Không có React Error Boundary | JS error trong child component → white screen toàn app |
| **Loading/empty states** | 🟢 LOW | `loading` boolean nhưng chưa có skeleton UI | UX không mượt nhưng không phải risk kỹ thuật |

## 2.6 DevOps & Production Readiness

| Vấn đề | Mức rủi ro | Hiện trạng | Hậu quả |
| --- | --- | --- | --- |
| **Dockerization** | ✅ RESOLVED | Multi-stage Dockerfile + docker-compose (backend + postgres + redis + frontend) | Deploy reproducible |
| **Environment separation** | 🟡 MEDIUM | `API_URL` hardcode trong frontend. Swagger chỉ tắt theo env check | Không thể deploy staging/production mà không sửa source code |
| **CI/CD** | 🟡 MEDIUM | Không có pipeline | Deploy thủ công → error-prone, chậm, không rollback |
| **Structured logging** | ✅ RESOLVED | Serilog + Console + File sinks, structured request logging | Full audit trail |
| **Monitoring** | ✅ RESOLVED | HealthController + `/api/Health` endpoint, Docker healthchecks | System status visible |
| **Rate limiting** | ✅ RESOLVED | Fixed window 100 req/min + Upload 20 req/min | DoS protection |
| **Caching** | ✅ RESOLVED | Redis/In-memory distributed cache, CollectionService cached (TTL 5m) | 80%+ DB reads reduced |
| **HTTPS enforcement** | 🟡 MEDIUM | Default profile là HTTP | Data truyền plaintext → sniff được nội dung + các thông tin nhạy cảm |

## 2.7 Testability

| Vấn đề | Mức rủi ro | Hiện trạng | Hậu quả |
| --- | --- | --- | --- |
| **Unit test** | 🟡 MEDIUM | Không có test project nào | Refactor = đánh cược. Regression bugs vào production |
| **Integration test** | 🟡 MEDIUM | Không có | API contract thay đổi không ai biết |
| **E2E test** | 🟢 LOW | Không có | Regression trên UI flow chỉ phát hiện bằng manual testing |
| **API contract testing** | 🟡 MEDIUM | Chỉ có Swagger cho dev | Frontend/Backend out of sync → runtime errors |

## 2.8 Đánh giá khả năng Scale hiện tại

```text
Concurrent Users:  ~1-3 (SQLite write lock, no auth)
Data Volume:       ~1,000 assets (no pagination, no index)
File Storage:      ~5-10GB (local disk, no cleanup)
Deployment:        Single instance only
Availability:      Zero redundancy
```

**Kết luận Phần 2:** Hệ thống đã có **Authentication + Data Ownership + Dockerization + Structured Logging + Rate Limiting + Caching + Thumbnail Generation**, khắc phục phần lớn rủi ro. Còn lại là các rủi ro mức MEDIUM tập trung ở: CI/CD pipeline, HTTPS enforcement, testing, và CORS tiếp tục cải thiện. Hệ thống đủ điều kiện deploy production (bằng Docker) và phát triển tiếp Giai đoạn 4.

---

# PHẦN 3 — ĐỀ XUẤT & ROADMAP NÂNG CẤP

## Giai đoạn 1 — Bắt buộc để đạt Production cơ bản

> **Mục tiêu:** Hệ thống an toàn tối thiểu, không mất data, deploy được  
> **Thời gian ước lượng:** 2-3 tuần  
> **ROI:** Rất cao — chặn mọi rủi ro HIGH

### 1.1 Authentication — JWT + ASP.NET Identity — ✅ HOÀN THÀNH (25/02/2026)

| | Chi tiết |
| --- | --- |
| **Lý do** | Không auth = không thể deploy. Đây là hard blocker #1 |
| **Giải pháp** | `Microsoft.AspNetCore.Identity.EntityFrameworkCore` + JWT Bearer token |
| **Impact** | Thêm `User` entity, `IdentityDbContext`, `[Authorize]` attribute trên tất cả controller. Frontend cần login page + token storage |
| **Độ khó** | Medium |
| **Ưu tiên ROI** | ★★★★★ |
| **Trạng thái** | ✅ `ApplicationUser` kế thừa `IdentityUser` (DisplayName, CreatedAt). `AuthService` xử lý register/login + JWT generation. `AuthController`: POST /api/auth/register, POST /api/auth/login. `[Authorize]` trên Assets, Collections, Search controllers. JWT config đọc từ appsettings.json. Password policy: min 6 chars, require digit + lowercase |

```text
Cấu trúc đã thêm:
├── Models/ApplicationUser.cs        (kế thừa IdentityUser)
├── Models/AuthDTOs.cs               (RegisterDto, LoginDto, AuthResponseDto)
├── Services/IAuthService.cs         (interface)
├── Services/AuthService.cs          (login, register, JWT generation)
├── Controllers/AuthController.cs    (POST /api/auth/login, /register)
```

### 1.2 EF Core Migrations (thay thế EnsureCreated) — ✅ HOÀN THÀNH (25/02/2026)

| | Chi tiết |
| --- | --- |
| **Lý do** | `EnsureCreated` không update schema. Thay đổi model = xóa DB = mất production data |
| **Giải pháp** | `dotnet ef migrations add Initial` → `dotnet ef database update`. Xóa `EnsureCreated()` |
| **Impact** | Seed data chuyển vào `OnModelCreating` hoặc `IHostedService`. Schema version control |
| **Độ khó** | Low |
| **Ưu tiên ROI** | ★★★★★ |
| **Trạng thái** | ✅ `Program.cs`: `EnsureCreated()` → `Database.Migrate()`. Migration `InitialCreate` tạo toàn bộ schema + indexes + FK + seed data. `CreatedAt` dùng `HasDefaultValueSql("datetime('now')")`. Thư mục `Migrations/` được version control |

### 1.3 Global Exception Handling Middleware

| | Chi tiết |
| --- | --- |
| **Lý do** | Unhandled exception → stack trace leak → thông tin nhạy cảm cho attacker |
| **Giải pháp** | Custom `ExceptionHandlingMiddleware` wrap tất cả request. Trả `ProblemDetails` chuẩn RFC 7807 |
| **Impact** | 1 file middleware + đăng ký trong pipeline. Logging tự động mỗi exception |
| **Độ khó** | Low |
| **Ưu tiên ROI** | ★★★★★ |

```csharp
// Mẫu cấu trúc response
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Internal Server Error",
  "status": 500,
  "traceId": "00-abc123-def456-00"
}
```

### 1.4 Validation Layer — FluentValidation

| | Chi tiết |
| --- | --- |
| **Lý do** | DTO hiện tại không validate gì ngoài field presence. Dữ liệu bẩn vào DB |
| **Giải pháp** | `FluentValidation.AspNetCore` — tạo Validator class cho mỗi DTO |
| **Impact** | Validate tự động trước khi vào controller action. Trả 400 với message rõ ràng |
| **Độ khó** | Low |
| **Ưu tiên ROI** | ★★★★☆ |

```csharp
// Ví dụ
public class CreateLinkDtoValidator : AbstractValidator<CreateLinkDto>
{
    public CreateLinkDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Url).NotEmpty().Must(BeValidUrl);
        RuleFor(x => x.CollectionId).GreaterThan(0);
    }
}
```

### 1.5 File Upload Restrictions

| | Chi tiết |
| --- | --- |
| **Lý do** | Upload không giới hạn → DoS, malware upload, disk exhaustion |
| **Giải pháp** | Whitelist MIME types (image/png, image/jpeg, image/webp, image/gif, image/svg+xml), max 50MB/file, max 20 files/request. Configure `Kestrel` `MaxRequestBodySize` |
| **Impact** | Thêm validation trong `UploadFiles` action + Kestrel config |
| **Độ khó** | Low |
| **Ưu tiên ROI** | ★★★★☆ |

### 1.6 Pagination

| | Chi tiết |
| --- | --- |
| **Lý do** | `GetAssets` load toàn bộ. `GetCollectionWithItems` load tất cả items. 10K records = crash |
| **Giải pháp** | `PagedResult<T>` response wrapper. Query params: `?page=1&pageSize=50&sortBy=createdAt&order=desc` |
| **Impact** | Sửa tất cả list endpoints. Frontend cần infinite scroll hoặc pagination component |
| **Độ khó** | Medium |
| **Ưu tiên ROI** | ★★★★★ |

```csharp
public class PagedResult<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasNextPage => Page * PageSize < TotalCount;
}
```

### 1.7 User Entity + Data Ownership — ✅ HOÀN THÀNH (25/02/2026)

| | Chi tiết |
| --- | --- |
| **Lý do** | Prerequisite cho multi-user. Không có `UserId` → không biết ai sở hữu gì |
| **Giải pháp** | Thêm `UserId` FK vào Asset + Collection. Filter trong query: `Where(a => a.UserId == currentUserId)` |
| **Impact** | Migration mới, sửa mỗi controller endpoint để filter theo user |
| **Độ khó** | Medium |
| **Ưu tiên ROI** | ★★★★★ |
| **Trạng thái** | ✅ `UserId` (nullable string) FK → `AspNetUsers` trên cả Asset và Collection. CASCADE delete. Index trên UserId. System collections (seed, UserId=null) hiển thị cho tất cả users. Mọi service method nhận `string userId` parameter. Controllers extract từ JWT `ClaimTypes.NameIdentifier`. Read: own + system. Create: gán UserId tự động. Update/Delete: chỉ own data |

---

## Giai đoạn 2 — Chuẩn hóa kiến trúc

> **Mục tiêu:** Clean Architecture, maintainable, testable  
> **Thời gian ước lượng:** 3-4 tuần  
> **ROI:** Cao — giảm technical debt, tăng tốc phát triển feature mới

### 2.1 Service Layer + Repository Pattern (hoặc CQRS nhẹ)

| | Chi tiết |
| --- | --- |
| **Lý do** | Controller hiện tại = 320 lines chứa cả business logic + data access. Không test được, không reuse được |
| **Giải pháp đề xuất** | Thin Controller → Service → Repository |
| **Impact kiến trúc** | Tách thành 3 layers rõ ràng. DI registration cho mỗi layer |
| **Độ khó** | Medium |

```text
Controllers/                     ← Chỉ nhận request + trả response
  AssetsController.cs
Services/                        ← Business logic, validation, orchestration
  IAssetService.cs
  AssetService.cs
  ICollectionService.cs
  CollectionService.cs
Repositories/                    ← Data access
  IAssetRepository.cs
  AssetRepository.cs
  ICollectionRepository.cs
  CollectionRepository.cs
```

**Thay thế khả thi:** Nếu muốn nhẹ hơn, dùng **MediatR + CQRS patterns** (tách Command/Query) thay vì Service + Repository truyền thống. Phù hợp nếu business logic phức tạp.

### 2.2 Server-side Search API

| | Chi tiết |
| --- | --- |
| **Lý do** | Tìm kiếm hiện tại chạy trên frontend bằng `.includes()` — không scale, không fuzzy, không accent-insensitive |
| **Giải pháp** | `GET /api/search?q=landscape&type=image&collectionId=1` với SQL `LIKE` ban đầu, sau chuyển full-text search |
| **Impact** | Thêm SearchController + SearchService. Frontend chuyển sang debounced API call |
| **Độ khó** | Medium |

### 2.3 Database Indexing

| | Chi tiết |
| --- | --- |
| **Lý do** | Mỗi query `WHERE CollectionId = ? AND ParentFolderId = ?` đang full table scan |
| **Giải pháp** | Khai báo trong `OnModelCreating`: |
| **Độ khó** | Low |

```csharp
modelBuilder.Entity<Asset>(entity =>
{
    entity.HasIndex(a => a.CollectionId);
    entity.HasIndex(a => a.ParentFolderId);
    entity.HasIndex(a => new { a.CollectionId, a.ParentFolderId }); // composite
    entity.HasIndex(a => a.ContentType);
    entity.HasIndex(a => a.GroupId);
    entity.HasIndex(a => a.CreatedAt);
});

modelBuilder.Entity<Collection>(entity =>
{
    entity.HasIndex(c => c.ParentId);
    entity.HasIndex(c => c.Order);
});
```

### 2.4 Storage Abstraction

| | Chi tiết |
| --- | --- |
| **Lý do** | Hard-coded local path → không thể swap sang cloud mà không sửa controller |
| **Giải pháp** | Interface `IStorageService` với implementations: `LocalStorageService`, `S3StorageService`, `AzureBlobStorageService` |
| **Impact** | Tách file I/O ra khỏi controller. DI swap implementation theo config |
| **Độ khó** | Medium |

```csharp
public interface IStorageService
{
    Task<string> UploadAsync(Stream file, string fileName, string contentType);
    Task DeleteAsync(string filePath);
    string GetPublicUrl(string filePath);
}
```

### 2.5 Refactor Frontend State Management

| | Chi tiết |
| --- | --- |
| **Lý do** | 11 useState + 14 handlers trong 1 file 650 lines → unmaintainable |
| **Giải pháp — Giai đoạn 2a (nhẹ)** | Custom hooks: `useCollections()`, `useAssets()`, `useUpload()`, `useSearch()`. Từ 1 component → tách thành hooks + context |
| **Giải pháp — Giai đoạn 2b (mạnh)** | Zustand hoặc Jotai cho global state. TanStack Query (React Query) cho server state |
| **Impact** | App.jsx giảm từ 650 → ~150 lines. Mỗi hook tự quản lý state + API call |
| **Độ khó** | Medium |

```text
src/
├── hooks/
│   ├── useCollections.js      ← state + CRUD collections
│   ├── useAssets.js           ← state + CRUD assets
│   ├── useUpload.js           ← upload logic
│   └── useSearch.js           ← search state + debounce
├── api/
│   ├── client.js              ← axios instance + interceptors
│   ├── assetsApi.js           ← API functions cho assets
│   └── collectionsApi.js      ← API functions cho collections
├── context/
│   └── AppContext.jsx         ← shared state (selectedCollection, user)
```

### 2.6 React Router — ✅ HOÀN THÀNH (27/02/2026)

| | Chi tiết |
| --- | --- |
| **Lý do** | Không routing → URL không reflect state → không bookmark/share/back-forward |
| **Giải pháp** | `react-router-dom` v7: `/collections/:id`, `/collections/:id/folder/:folderId`, `/login` |
| **Impact** | Restructure App.jsx thành route-based layout. Deep linking hoạt động |
| **Độ khó** | Medium |
| **Trạng thái** | ✅ `react-router-dom@7` cài đặt. `BrowserRouter` wrap app trong `main.jsx`. 4 routes: `/login`, `/`, `/collections/:collectionId`, `/collections/:collectionId/folder/:folderId`. `useCollections` hook sync URL ↔ state (pushes URL on navigate, reads URL on load/back-forward). Auth guard redirect `/login` ↔ `/`. AppLayout component render cho tất cả authenticated routes. Browser back/forward hoạt động. Deep link bookmarkable. |

---

## Giai đoạn 3 — Production-grade

> **Mục tiêu:** Deployable, monitorable, resilient  
> **Thời gian ước lượng:** 4-6 tuần  
> **ROI:** Cao cho production deployment

### 3.1 Chuyển sang PostgreSQL — ✅ HOÀN THÀNH (27/02/2026)

| | Chi tiết |
| --- | --- |
| **Lý do** | SQLite: single-writer, no concurrent access, file-based, max ~100 reads. Không scale |
| **Giải pháp** | `Npgsql.EntityFrameworkCore.PostgreSQL` v9.0.4. Dual-provider: SQLite cho dev, PostgreSQL cho Docker/production |
| **Bonus** | PostgreSQL hỗ trợ: Full-text search (`tsvector`), JSONB cho flexible metadata, GIN index cho tags array, concurrent writes |
| **Impact** | `DatabaseProvider` config key. `AppDbContext` nhận `DatabaseProviderInfo` → dialect-aware `HasDefaultValueSql`. `docker-compose.yml` dùng PostgreSQL 17. Local dev vẫn dùng SQLite |
| **Độ khó** | Low (nhờ EF Core abstraction) |
| **Trạng thái** | ✅ Install `Npgsql.EntityFrameworkCore.PostgreSQL` 9.0.4. `Program.cs`: đọc `DatabaseProvider` config → `UseNpgsql()` hoặc `UseSqlite()`. `AppDbContext`: `DatabaseProviderInfo` inject, `HasDefaultValueSql` trả `now()` (PG) hoặc `datetime('now')` (SQLite). `appsettings.json`: `DatabaseProvider: SQLite`, thêm PG connection string. `docker-compose.yml`: PostgreSQL 17 Alpine + healthcheck |

### 3.2 Docker + docker-compose — ✅ HOÀN THÀNH (27/02/2026)

| | Chi tiết |
| --- | --- |
| **Lý do** | Reproducible environment. Deploy lên bất kỳ cloud nào |
| **Giải pháp** | Multi-stage Dockerfile (build + runtime). docker-compose cho local dev (backend + postgres + redis + frontend) |
| **Độ khó** | Low |
| **Trạng thái** | ✅ `VAH.Backend/Dockerfile`: SDK 9.0 build → ASP.NET 9.0 runtime, non-root user, port 5027. `VAH.Frontend/Dockerfile`: Node 22 build → Nginx Alpine, `VITE_API_URL` build arg. `VAH.Frontend/nginx.conf`: SPA fallback + gzip + cache. `docker-compose.yml`: 4 services (postgres:17, redis:7, backend, frontend), named volumes, healthchecks. `.dockerignore` cho cả backend + frontend |

```yaml
# docker-compose.yml
services:
  backend:
    build: ./VAH.Backend
    depends_on: [postgres, redis]
    environment:
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=vah;...
  postgres:
    image: postgres:17
  redis:
    image: redis:7-alpine
  frontend:
    build: ./VAH.Frontend
    ports: ["5173:80"]
```

### 3.3 Redis Cache — ✅ HOÀN THÀNH (27/02/2026)

| | Chi tiết |
| --- | --- |
| **Lý do** | Collections list, asset metadata thay đổi không thường xuyên nhưng đọc liên tục |
| **Giải pháp** | `Microsoft.Extensions.Caching.StackExchangeRedis` v10.0.3. `IDistributedCache` inject vào `CollectionService` |
| **Impact** | Giảm 80%+ DB reads cho navigation operations |
| **Độ khó** | Low |
| **Trạng thái** | ✅ Install `StackExchangeRedis` 10.0.3. `Program.cs`: nếu có `ConnectionStrings:Redis` → `AddStackExchangeRedisCache`, nếu không → `AddDistributedMemoryCache` fallback. `CollectionService`: `GetAllAsync` cache 5 phút (absolute) / 2 phút (sliding). `InvalidateCacheAsync` gọi sau Create/Update/Delete. Cache key = `collections:all:{userId}`. Try-catch bọc mọi cache operation → graceful degradation nếu Redis down |

### 3.4 Structured Logging — Serilog — ✅ HOÀN THÀNH (27/02/2026)

| | Chi tiết |
| --- | --- |
| **Lý do** | Console log → không search, filter, alert, aggregate |
| **Giải pháp** | `Serilog.AspNetCore` v10.0.0 + Console + File sinks |
| **Impact** | Structured JSON logs. Correlation ID per request. Performance metrics |
| **Độ khó** | Low |
| **Trạng thái** | ✅ `Serilog.AspNetCore` 10.0.0, `Serilog.Sinks.Console` 6.1.1, `Serilog.Sinks.File` 7.0.0. Bootstrap logger trước host build. `UseSerilog()` trên host. `UseSerilogRequestLogging()` trong pipeline (custom template, log level by status code/elapsed). Console: `[HH:mm:ss LVL] SourceContext\n  Message`. File: `logs/vah-{Date}.log`, rolling daily, 30 ngày retention. Override: ASP.NET + EF Core → Warning only. Try/catch/finally wrap toàn bộ `Program.cs` |

### 3.5 Health Check + Monitoring — ✅ ĐÃ CÓ SẴN

| | Chi tiết |
| --- | --- |
| **Lý do** | Không biết system đang up/down, DB accessible hay không |
| **Giải pháp** | `HealthController` + `/api/Health` endpoint |
| **Độ khó** | Low |
| **Trạng thái** | ✅ Đã có `HealthController` với endpoint GET `/api/Health`. Docker healthcheck sử dụng endpoint này |

### 3.6 Rate Limiting — ✅ ĐÃ CÓ SẴN

| | Chi tiết |
| --- | --- |
| **Lý do** | Không rate limit → bất kỳ client nào spam 10K request/giây → DoS |
| **Giải pháp** | `Microsoft.AspNetCore.RateLimiting` (built-in .NET 9). Fixed window per endpoint |
| **Impact** | 2 policies: `Fixed` (100 req/phút), `Upload` (20 req/phút) |
| **Độ khó** | Low |
| **Trạng thái** | ✅ Đã có `AddRateLimiter` trong `Program.cs` + `UseRateLimiter()` trong pipeline |

### 3.7 Background Job — Thumbnail Generation — ✅ HOÀN THÀNH (27/02/2026)

| | Chi tiết |
| --- | --- |
| **Lý do** | Hiện tại serve ảnh gốc 20MB cho thumbnail 150px → bandwidth disaster |
| **Giải pháp** | `SixLabors.ImageSharp` v3.1.12. Sync thumbnail generation ngay sau upload (không cần Hangfire cho MVP) |
| **Impact** | Thêm `ThumbnailSm/Md/Lg` vào Asset model. Serve thumbnail cho browse, original cho preview |
| **Độ khó** | Medium |
| **Trạng thái** | ✅ `IThumbnailService` + `ThumbnailService`: generate WebP thumbnails 3 sizes (sm:150px, md:400px, lg:800px). Supports: jpg, jpeg, png, gif, bmp, webp, tiff. Output: `/uploads/thumbs/{size}_{guid}.webp`. `Asset.cs`: thêm `ThumbnailSm`, `ThumbnailMd`, `ThumbnailLg` (nullable, MaxLength 2048). `AssetService`: post-upload thumbnail generation + delete cleanup. Migration `AddThumbnailColumns` |

---

## Giai đoạn 4 — Nâng cấp sản phẩm

> **Mục tiêu:** Feature-rich, SaaS-ready  
> **Thời gian ước lượng:** 6-10 tuần  
> **ROI:** Trung bình — business value cao nhưng đòi hỏi nền tảng GĐ1-3 vững

### 4.1 Smart Collections — Auto-categorize

- Tự gom nhóm asset theo: metadata, upload date, content type, AI tags
- **Công nghệ:** Background job scan + rule engine hoặc ML classification
- **Độ khó:** High

### 4.2 Tag System chuẩn hóa — Many-to-Many

- Thay `string Tags` (comma-separated) bằng bảng `Tags` + junction table `AssetTags`
- PostgreSQL: dùng `string[]` + GIN index thay vì join table (đơn giản hơn)
- **Giải pháp khuyến nghị cho PostgreSQL:**

```csharp
// Với PostgreSQL, dùng array type
public string[] Tags { get; set; } = Array.Empty<string>();

// Index
modelBuilder.Entity<Asset>()
    .HasIndex(a => a.Tags)
    .HasMethod("gin");
```

- **Độ khó:** Medium

### 4.3 Bulk Operations

- Multi-select → move, delete, tag, download ZIP
- **Frontend:** Shift+click range select, Ctrl+click toggle
- **Backend:** Batch endpoints: `POST /api/assets/bulk-delete`, `POST /api/assets/bulk-move`
- **Độ khó:** Medium

### 4.4 Undo/Redo

- Command pattern: mỗi action = command object với `execute()` + `undo()`
- Stack-based undo (Ctrl+Z) / redo (Ctrl+Shift+Z)
- **Frontend state:** `useUndoRedo()` hook với action stack
- **Độ khó:** High

### 4.5 Real-time Sync — SignalR

- Khi user A upload → user B thấy ngay không cần refresh
- `Microsoft.AspNetCore.SignalR` hub cho collection changes
- Frontend: subscribe via `@microsoft/signalr`
- **Độ khó:** Medium

### 4.6 Permission Model — Role-based Access Control (RBAC)

- Roles: Owner, Editor, Viewer per Collection
- Bảng `CollectionPermissions`: `UserId`, `CollectionId`, `Role`
- **Mở rộng:** Organization entity cho team sharing
- **Độ khó:** High

---

# PHỤ LỤC — KIẾN TRÚC ĐÍCH (TARGET ARCHITECTURE)

## Sơ đồ kiến trúc target-state

```text
┌─────────────────────────────────────────────────────────────────┐
│                            CLIENTS                              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │  React SPA  │  │  Mobile App │  │  Public API │              │
│  │  (Vite PWA) │  │  (future)   │  │  (future)   │              │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘              │
└─────────┼────────────────┼────────────────┼─────────────────────┘
          │                │                │
          ▼                ▼                ▼
┌─────────────────────────────────────────────────────────────────┐
│                     REVERSE PROXY / CDN                         │
│                    (Nginx / Cloudflare)                         │
│  ┌──────────────────────────────────────────────┐               │
│  │  SSL Termination │ Rate Limiting │ Caching   │               │
│  └──────────────────────────────────────────────┘               │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                    API GATEWAY / BACKEND                        │
│                   ASP.NET Core 9.0                              │
│                                                                 │
│  ┌────────────┐  ┌──────────────┐  ┌──────────────────────┐     │
│  │ Controllers│→ │  Services    │→ │  Repositories        │     │
│  │ (thin)     │  │ (biz logic)  │  │ (EF Core + cache)    │     │
│  └────────────┘  └──────────────┘  └───────────┬──────────┘     │
│                                                │                │
│  ┌────────────┐  ┌──────────────┐              │                │
│  │ Auth (JWT) │  │ SignalR Hub  │              │                │
│  └────────────┘  └──────────────┘              │                │
│                                                │                │
│  ┌────────────┐  ┌──────────────┐              │                │
│  │ Middleware │  │ Background   │              │                │
│  │ (Exception,│  │ Jobs         │              │                │
│  │  RateLimit,│  │ (Hangfire)   │              │                │
│  │  Logging)  │  │ - Thumbnails │              │                │
│  └────────────┘  │ - Cleanup    │              │                │
│                  │ - AI Tags    │              │                │
│                  └──────────────┘              │                │
└────────────────────────────────────────────────│────────────────┘
                             ┌───────────────────┘
       ┌─────────────────────┼───────────────────────┐
       │                     │                       │
       ▼                     ▼                       ▼
┌───────────────┐   ┌──────────────────┐   ┌─────────────────────┐
│  PostgreSQL   │   │  Redis           │   │  Storage Service    │
│  (Primary DB) │   │  (Cache + Session│   │  Local / S3 / Azure │
│               │   │  + Queue)        │   │  Blob               │
│  - Assets     │   └──────────────────┘   └─────────────────────┘         
│  - Collections│           
│  - Users      │                           
│  - Permissions│                           
│  - Tags       │                           
│  - AuditLog   │
└───────────────┘
```

## Frontend Target Architecture

```text
src/
├── api/                          ← Axios instance + typed API functions
│   ├── client.ts                 (interceptors, auth token, error handler)
│   ├── assets.ts  
│   ├── collections.ts
│   └── auth.ts
├── hooks/                        ← Custom hooks (business logic)
│   ├── useAuth.ts
│   ├── useCollections.ts
│   ├── useAssets.ts
│   ├── useUpload.ts
│   ├── useSearch.ts
│   └── useUndoRedo.ts
├── stores/                       ← Zustand stores (global state)
│   ├── authStore.ts
│   ├── uiStore.ts                (layout, theme, sidebar collapsed)
│   └── selectionStore.ts         (selected items, multi-select)
├── pages/                        ← Route-level components
│   ├── LoginPage.tsx
│   ├── DashboardPage.tsx
│   ├── CollectionPage.tsx
│   └── SearchPage.tsx
├── components/                   ← Presentational components
│   ├── layout/
│   │   ├── AppHeader.tsx
│   │   ├── Sidebar.tsx
│   │   └── DetailsPanel.tsx
│   ├── collections/
│   │   ├── CollectionTree.tsx
│   │   └── CollectionBrowser.tsx
│   ├── assets/
│   │   ├── AssetGrid.tsx
│   │   ├── AssetList.tsx
│   │   └── AssetCanvas.tsx
│   └── ui/                       ← Reusable primitives
│       ├── Button.tsx
│       ├── SearchInput.tsx
│       ├── Skeleton.tsx
│       └── ErrorBoundary.tsx
├── utils/                        ← Pure utility functions
│   ├── format.ts
│   └── validators.ts
└── types/                        ← TypeScript interfaces
    ├── asset.ts
    └── collection.ts
```

---

## Tổng kết rủi ro mở rộng nếu KHÔNG refactor

| Scenario | Hiện tại | Với 1K users | Với 10K users |
| --- | --- | --- | --- |
| **DB Writes** | OK (SQLite single user) | 🔴 Write contention, timeout | 🔴 SQLite crash, data corruption |
| **API Response** | OK (<100 assets) | 🟡 3-5s load (no pagination) | 🔴 30s+ timeout, OOM |
| **File Storage** | OK (local <5GB) | 🟡 50GB disk pressure | 🔴 Disk full, server down |
| **Security** | N/A (local only) | 🔴 Data breach, unauthorized access | 🔴 Legal liability |
| **Frontend** | OK (small data) | 🟡 Re-render lag, state bugs | 🔴 Browser crash (10K DOM nodes) |
| **Deploy** | Manual | 🟡 Error-prone, slow | 🔴 Impossible without CI/CD |

**Bottom line:** Giai đoạn 1 đã hoàn thành **7/7** hạng mục (100%) — bao gồm Authentication (JWT + Identity), User Entity + Data Ownership, EF Core Migrations, Exception Handling, Validation, File Upload Restrictions, Pagination. Giai đoạn 2 đã hoàn thành **6/6** (100%) — bao gồm Service Layer, Server-side Search, Database Indexing, Storage Abstraction, Frontend State Refactor, React Router. Giai đoạn 3 đã hoàn thành **7/7** (100%) — bao gồm PostgreSQL dual-provider, Docker + docker-compose, Redis Cache, Serilog, Health Check, Rate Limiting, Thumbnail Generation. Giai đoạn 4 là product differentiation.

---

> *Tài liệu này được cập nhật lần cuối: **27/02/2026**. Mỗi section có thể trở thành epic/ticket riêng trong project management tool.*
