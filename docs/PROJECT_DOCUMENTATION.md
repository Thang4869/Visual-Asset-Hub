# Visual Asset Hub — Tài liệu Kỹ thuật Dự án

> Cập nhật lần cuối: 01/03/2026  
> Đây là tài liệu kỹ thuật nguồn sự thật cho trạng thái code hiện tại.
> Thuật ngữ dùng chung: `docs/GLOSSARY.md`

---

## 1. Cấu trúc repository

```text
1A/
├─ docker-compose.yml
├─ README.md
├─ docs/
├─ VAH.Backend/
│  ├─ Controllers/
│  ├─ Features/Assets/
│  ├─ Services/
│  ├─ Data/
│  ├─ Models/
│  ├─ Middleware/
│  ├─ Hubs/
│  └─ Migrations/
└─ VAH.Frontend/
   ├─ src/
   ├─ public/
   └─ package.json
```

---

## 2. Backend runtime

## 2.1 Nền tảng

- ASP.NET Core 9
- EF Core 9
- ASP.NET Identity + JWT
- SignalR
- Serilog
- Swagger

## 2.2 Cơ sở dữ liệu

- Development: SQLite
- Production: PostgreSQL
- Cơ chế chọn provider: `DatabaseProvider`

## 2.3 Cache

- Redis distributed cache nếu có cấu hình
- Fallback về in-memory distributed cache nếu không có Redis

## 2.4 Khởi động

- Auto migrate bằng `Database.Migrate()`
- Chạy script SQL chuẩn hóa discriminator `ContentType` cho dữ liệu cũ

---

## 3. Bề mặt API hiện tại

Toàn bộ route chuẩn hóa theo `api/v1/*`.

## 3.1 Assets

### Query

- `GET /api/v1/assets`
- `GET /api/v1/assets/{id}`
- `GET /api/v1/assets/group/{groupId}`

### Command

- `POST /api/v1/assets`
- `POST /api/v1/assets/upload`
- `PATCH /api/v1/assets/{id}`
- `PUT /api/v1/assets/{id}` (tương thích ngược)
- `DELETE /api/v1/assets/{id}`
- `POST /api/v1/assets/{id}/duplicate`
- `POST /api/v1/assets/{id}/duplicate-to-folder/{folderId}` (tương thích ngược)

### Layout + Bulk + Typed creators

- `PUT /api/v1/assets/{id}/position`
- `POST /api/v1/assets/reorder`
- `POST /api/v1/assets/bulk-delete`
- `POST /api/v1/assets/bulk-move`
- `POST /api/v1/assets/bulk-move-group`
- `POST /api/v1/assets/bulk-tag`
- `POST /api/v1/assets/folders`
- `POST /api/v1/assets/colors`
- `POST /api/v1/assets/color-groups`
- `POST /api/v1/assets/links`

## 3.2 Các domain khác

- Auth: `api/v1/auth/*`
- Collections: `api/v1/collections/*`
- Permissions: `api/v1/collections/{collectionId}/permissions/*` + `api/v1/shared-collections`
- Tags: `api/v1/tags/*`
- SmartCollections: `api/v1/smartcollections/*`
- Search: `api/v1/search`
- Health: `api/v1/health`

---

## 4. Hợp đồng kiến trúc Assets

## 4.1 Ranh giới tầng trình bày (Presentation Boundary)

- Request model upload: `UploadAssetsRequest`
- Request model duplicate: `DuplicateAssetRequest`
- Controller không tự quản lý mở stream

## 4.2 Ranh giới tầng ứng dụng (Application Boundary)

- `IAssetApplicationService` là facade ứng dụng cho controller
- `IUserContextProvider` là nguồn duy nhất lấy UserId hiện hành
- `AssetOptions` quản lý default collection theo cấu hình

## 4.3 Chiến lược nhân bản (Duplicate Strategy)

- `IAssetDuplicateStrategyFactory` quyết định chiến lược
- `InPlaceDuplicateStrategy`
- `TargetFolderDuplicateStrategy`

---

## 5. Quy ước kỹ thuật

- Route versioning: `api/v1`
- Policy:
  - `RequireAssetRead`
  - `RequireAssetWrite`
- Error response: ProblemDetails (RFC 7807)
- Enum JSON: kebab-case-lower

---

## 6. Trạng thái migrate kiến trúc

Hệ thống đang ở trạng thái chuyển đổi có kiểm soát:

- Assets đã lát cắt tính năng hoàn chỉnh
- Các domain còn lại vẫn theo layer truyền thống

Cách làm này giúp giảm rủi ro khi chuyển đổi lớn và vẫn giữ nhịp phát triển tính năng.
