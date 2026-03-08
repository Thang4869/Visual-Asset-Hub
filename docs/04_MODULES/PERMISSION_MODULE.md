# PERMISSION MODULE

> **Last Updated**: 2026-03-08
> **Status**: Active — Services/ layer

---

## §1 — Overview

| Aspect | Detail |
|--------|--------|
| **Domain** | Role-based access control for shared collections |
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

| Method | Purpose |
|--------|---------|
| `CanWrite` | True if role is owner or editor |
| `CanManage` | True if role is owner |
| `SetRole(role)` | Validates role ∈ {owner, editor, viewer}, throws on invalid |

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

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/v1/permissions/{collectionId}` | List permissions for a collection |
| POST | `/api/v1/permissions/{collectionId}` | Grant permission (by user email) |
| PUT | `/api/v1/permissions/{permissionId}` | Update permission role |
| DELETE | `/api/v1/permissions/{permissionId}` | Revoke permission |
| GET | `/api/v1/permissions/my-role/{collectionId}` | Get current user's role |
| GET | `/api/v1/permissions/shared-collections` | Get collections shared with me |

## §5 — Permission Grant Flow

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

## §6 — Authorization Rules

| Action | Required Role |
|--------|--------------|
| View collection assets | viewer+ |
| Add/edit assets | editor+ |
| Delete assets | editor+ |
| Grant/revoke permissions | owner only |
| Delete collection | owner only |

---

> **Document End**
