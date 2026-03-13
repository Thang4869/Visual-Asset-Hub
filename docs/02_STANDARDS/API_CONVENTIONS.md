# API CONVENTIONS — REST API Design Rules

> **Last Updated**: 2026-03-08  
> **Base URL**: `/api/v1`

---

## §1 — URL Design

### Pattern
```
/api/v1/{resource}              → Collection (GET list, POST create)
/api/v1/{resource}/{id}         → Item (GET, PATCH/PUT, DELETE)
/api/v1/{resource}/{id}/{sub}   → Sub-resource or action
```

### Current Endpoints (60 total)

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

## §2 — HTTP Methods & Status Codes

| Action | Method | Success | Error |
|--------|--------|---------|-------|
| List resources | `GET` | 200 + array/paged | 401 |
| Get single | `GET` | 200 | 404 |
| Create | `POST` | 201 + `Location` header | 400, 409 |
| Full update | `PUT` | 200 | 400, 404 |
| Partial update | `PATCH` | 200 | 400, 404 |
| Delete | `DELETE` | 204 (no body) | 404 |
| Action (bulk) | `POST` | 200 + count | 400 |

## §3 — Request/Response Format

All responses use `application/json`. Errors follow RFC 7807 ProblemDetails:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "detail": "Asset with ID '42' was not found."
}
```

Validation errors include field-level detail:
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

## §4 — Pagination

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

## §5 — Authentication

- **Method**: JWT Bearer token
- **Header**: `Authorization: Bearer {token}`
- **SignalR**: Token via query string `?access_token={token}`
- **Token lifetime**: Configured in `appsettings.json` → `Jwt:*`
- **401 handling**: Frontend clears token + reloads on 401

## §6 — Rate Limiting

| Policy | Limit | Window | Applied To |
|--------|-------|--------|------------|
| `Fixed` | 100 requests | 1 minute | `AuthController`, `TagsController` (migrate) |
| `Upload` | 20 requests | 1 minute | File upload endpoints |
| `Search` | 60 requests (sliding) | 1 minute (6 segments) | `SearchController` |

## §7 — Versioning

---
**Ghi chú 2026-03-13:**
- Đã cập nhật các endpoint tags/assets cho chuẩn hóa RESTful, đồng bộ với migration và service mới.
- Các thay đổi về domain model, migration, và bảo mật đã được cập nhật trong tài liệu này.

- Current: `/api/v1/` (URL prefix)
- Strategy: URL-based versioning (simplest for SPA consumption)
- Breaking change = new version (`/api/v2/`)

---

> **Document End**
