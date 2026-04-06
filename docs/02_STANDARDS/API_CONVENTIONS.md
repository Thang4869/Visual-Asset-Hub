# Quy Ước API (API Conventions)

> **Mục đích**: Định nghĩa quy tắc thiết kế REST API cho hệ thống  
> **Last Updated**: 2026-04-06  
> **Base URL**: `/api/v1`

---

## §1 — Thiết Kế URL

### Mẫu URL (Pattern)
```
/api/v1/{resource}              → Collection (GET list, POST create)
/api/v1/{resource}/{id}         → Item (GET, PATCH/PUT, DELETE)
/api/v1/{resource}/{id}/{sub}   → Sub-resource or action
```

### Danh Sách Endpoints Hiện Tại (60 endpoints)

| Controller | Route Prefix | Endpoints | Auth |
|-----------|-------------|-----------|------|
| `AssetsQueryController` | `/api/v1/assets` | GET (list), GET `{id}`, GET `group/{groupId}` | `RequireAssetRead` |
| `AssetsCommandController` | `/api/v1/assets` | POST, POST `upload`, PATCH `{id}`, PUT `{id}`, DELETE `{id}`, POST `{id}/duplicate` | `RequireAssetWrite` |
| `AssetLayoutController` | `/api/v1/assets` | PUT `{id}/position`, POST `reorder` | `RequireAssetWrite` |
| `FoldersController` | `/api/v1/assets/folders` | POST | `RequireAssetWrite` |
| `ColorsController` | `/api/v1/assets/colors` | POST | `RequireAssetWrite` |
| `ColorGroupsController` | `/api/v1/assets/color-groups` | POST | `RequireAssetWrite` |
| `LinksController` | `/api/v1/assets/links` | POST | `RequireAssetWrite` |
| `BulkAssetsController` | `/api/v1/assets` | POST `bulk-delete`, `bulk-move`, `bulk-move-group`, `bulk-tag` | `RequireAssetWrite` |
| `CollectionsController` | `/api/v1/collections` | GET, GET `{id}`, GET `{id}/items`, POST, PATCH `{id}`, PUT `{id}`, DELETE `{id}` | `[Authorize]` |
| `TagsController` | `/api/v1/tags` | GET, GET `{id}`, POST, PUT `{id}`, DELETE `{id}`, GET `assets/{assetId}`, PUT `assets/{assetId}`, POST `assets/{assetId}` (add), DELETE `assets/{assetId}` (remove), POST `get-or-create`, PUT `assets/{assetId}` (set), POST `migrate` | `[Authorize]` |
| `SearchController` | `/api/v1/search` | GET `?q=&type=&collectionId=&page=&pageSize=` | `[Authorize]` |
| `SmartCollectionsController` | `/api/v1/smartcollections` | GET, GET `{id}/items` | `[Authorize]` |
| `PermissionsController` | `/api/v1/collections/{id}/permissions` | GET, POST, PUT `{permId}`, DELETE `{permId}`, GET `my-role` | `[Authorize]` |
| `SharedCollectionsController` | `/api/v1/shared-collections` | GET | `[Authorize]` |
| `AuthController` | `/api/v1/auth` | POST `register` (201), POST `login` | Rate-limited |
| `HealthController` | `/api/v1/health` | GET, GET `live` | Public |

---

## §2 — Phương Thức HTTP & Mã Trạng Thái

| Hành động | Method | Thành công | Lỗi |
|-----------|--------|------------|-----|
| Liệt kê tài nguyên | `GET` | 200 + array/paged | 401 |
| Lấy chi tiết | `GET` | 200 | 404 |
| Tạo mới | `POST` | 201 + `Location` header | 400, 409 |
| Cập nhật toàn bộ | `PUT` | 200 | 400, 404 |
| Cập nhật một phần | `PATCH` | 200 | 400, 404 |
| Xóa | `DELETE` | 204 (no body) | 404 |
| Thao tác hàng loạt | `POST` | 200 + count | 400 |

---

## §3 — Định Dạng Request/Response

Tất cả response sử dụng `application/json`. Lỗi tuân theo RFC 7807 ProblemDetails:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "detail": "Asset with ID '42' was not found."
}
```

Lỗi validation bao gồm chi tiết theo từng field:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation Error",
  "status": 400,
  "errors": {
    "FileName": ["The FileName field is required."],
    "CollectionId": ["Must be >= 1."]
  }
}
```

---

## §4 — Phân Trang

```
GET /api/v1/assets?page=1&pageSize=50

Response:
{
  "items": [...],
  "totalCount": 150,
  "page": 1,
  "pageSize": 50
}
```

---

## §5 — Xác Thực

- **Phương thức**: JWT Bearer token
- **Header**: `Authorization: Bearer {token}`
- **SignalR**: Token qua query string `?access_token={token}`
- **Thời hạn token**: Cấu hình trong `appsettings.json` → `Jwt:*`
- **Xử lý 401**: Frontend xóa token + reload trang khi nhận 401

---

## §6 — Giới Hạn Tốc Độ (Rate Limiting)

| Policy | Giới hạn | Thời gian | Áp dụng cho |
|--------|----------|-----------|-------------|
| `Fixed` | 100 requests | 1 phút | `AuthController`, `TagsController` (migrate) |
| `Upload` | 20 requests | 1 phút | File upload endpoints |
| `Search` | 60 requests (sliding) | 1 phút (6 segments) | `SearchController` |

---

## §7 — Phiên Bản API

**Ghi chú 2026-03-13:**
- Đã cập nhật các endpoint tags/assets cho chuẩn hóa RESTful, đồng bộ với migration và service mới.
- Các thay đổi về domain model, migration, và bảo mật đã được cập nhật trong tài liệu này.

- Hiện tại: `/api/v1/` (URL prefix)
- Chiến lược: URL-based versioning (đơn giản nhất cho SPA)
- Breaking change = phiên bản mới (`/api/v2/`)

---

> **Document End**
