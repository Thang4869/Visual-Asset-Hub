using VAH.Backend.Models;

namespace VAH.Backend.Services;

public interface IPermissionService
{
    /// <summary>Check if user has at least the specified role for a collection.</summary>
    Task<bool> HasPermissionAsync(int collectionId, string userId, string minimumRole, CancellationToken ct = default);

    /// <summary>Get user's role for a collection. Returns null if no permission.</summary>
    Task<string?> GetRoleAsync(int collectionId, string userId, CancellationToken ct = default);

    /// <summary>Grant permission to a user by email.</summary>
    Task<CollectionPermission> GrantAsync(int collectionId, GrantPermissionDto dto, string grantedByUserId, CancellationToken ct = default);

    /// <summary>Update an existing permission.</summary>
    Task<CollectionPermission> UpdateAsync(int permissionId, UpdatePermissionDto dto, string currentUserId, CancellationToken ct = default);

    /// <summary>Revoke a permission.</summary>
    Task<bool> RevokeAsync(int permissionId, string currentUserId, CancellationToken ct = default);

    /// <summary>List all permissions for a collection.</summary>
    Task<List<PermissionInfoDto>> ListAsync(int collectionId, string currentUserId, CancellationToken ct = default);

    /// <summary>Get all collections shared with a user (not owned by them).</summary>
    Task<List<Collection>> GetSharedCollectionsAsync(string userId, CancellationToken ct = default);
}
