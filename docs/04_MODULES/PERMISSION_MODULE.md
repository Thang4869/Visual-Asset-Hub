# Permission Module

> **Mục đích**: Kiểm soát truy cập dựa trên vai trò cho các collection được chia sẻ
> **Last Updated**: 2026-04-06

---

## §1 — Tổng quan (Overview)

| Khía cạnh | Chi tiết |
|-----------|----------|
| **Domain** | Kiểm soát truy cập dựa trên vai trò cho các collection được chia sẻ |
| **Entity** | `CollectionPermission` |
| **Roles** | `CollectionRoles`: owner, editor, viewer |
| **Service** | `IPermissionService` → `PermissionService` |
| **Controller** | `PermissionsController` (6 endpoints) |
| **Patterns** | Role hierarchy, static role constants, domain validation |

## §2 — Domain Model

```csharp
public class CollectionPermission
{
    int Id                    // PK
    string UserId             // Required — grantee
    int CollectionId          // FK to Collection
    string Role               // "owner" | "editor" | "viewer"
    string? GrantedBy         // Who granted this permission
    DateTime GrantedAt
}
```

**Domain Methods:**

| Method | Mục đích |
|--------|----------|
| `CanWrite` | True nếu role là owner hoặc editor |
| `CanManage` | True nếu role là owner |
| `SetRole(role)` | Xác thực role ∈ {owner, editor, viewer}, throw nếu không hợp lệ |

**Role Hierarchy (`CollectionRoles`):**

```
owner   → CanWrite ✅, CanManage ✅
editor  → CanWrite ✅, CanManage ❌
viewer  → CanWrite ❌, CanManage ❌
```

## §3 — Service Interface

```csharp
public interface IPermissionService
{
    Task<bool> HasPermissionAsync(int collectionId, string userId, string minimumRole, CancellationToken ct);
    Task<string?> GetRoleAsync(int collectionId, string userId, CancellationToken ct);
    Task<CollectionPermission> GrantAsync(int collectionId, GrantPermissionDto dto, string grantedByUserId, CancellationToken ct);
    Task<CollectionPermission> UpdateAsync(int permissionId, UpdatePermissionDto dto, string currentUserId, CancellationToken ct);
    Task<bool> RevokeAsync(int permissionId, string currentUserId, CancellationToken ct);
    Task<List<PermissionInfoDto>> ListAsync(int collectionId, string currentUserId, CancellationToken ct);
    Task<List<Collection>> GetSharedCollectionsAsync(string userId, CancellationToken ct);
}
```

## §4 — API Endpoints

| Method | Route | Mô tả |
|--------|-------|-------|
| GET | `/api/v1/permissions/{collectionId}` | Lấy danh sách permissions cho một collection |
| POST | `/api/v1/permissions/{collectionId}` | Cấp permission (theo email người dùng) |
| PUT | `/api/v1/permissions/{permissionId}` | Cập nhật role của permission |
| DELETE | `/api/v1/permissions/{permissionId}` | Thu hồi permission |
| GET | `/api/v1/permissions/my-role/{collectionId}` | Lấy role của người dùng hiện tại |
| GET | `/api/v1/permissions/shared-collections` | Lấy các collection được chia sẻ với tôi |

## §5 — Luồng cấp quyền (Permission Grant Flow)

```
Client             PermissionsController    IPermissionService    UserManager
  │                       │                       │                   │
  │── POST /permissions ─→│                       │                   │
  │  {collectionId,       │                       │                   │
  │   userEmail, role}    │── GrantAsync ────────→│                   │
  │                       │                       │── FindByEmail ───→│
  │                       │                       │←── targetUser ────│
  │                       │                       │── Verify grantor  │
  │                       │                       │   has CanManage   │
  │                       │                       │── Create record   │
  │                       │                       │── SaveChanges     │
  │                       │←── PermissionDto ─────│                   │
  │←── 201 Created ───────│                       │                   │
```

## §6 — Quy tắc phân quyền (Authorization Rules)

| Hành động | Role yêu cầu |
|-----------|--------------|
| Xem assets của collection | viewer+ |
| Thêm/sửa assets | editor+ |
| Xóa assets | editor+ |
| Cấp/thu hồi permissions | chỉ owner |
| Xóa collection | chỉ owner |

---

> **Document End**
