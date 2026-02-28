using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using VAH.Backend.Data;
using VAH.Backend.Models;

namespace VAH.Backend.Services;

public class PermissionService : IPermissionService
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<PermissionService> _logger;
    private readonly IDistributedCache _cache;

    public PermissionService(AppDbContext context, UserManager<ApplicationUser> userManager, ILogger<PermissionService> logger, IDistributedCache cache)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
        _cache = cache;
    }

    /// <summary>Invalidate a user's cached collection list so they see permission changes immediately.</summary>
    private async Task InvalidateUserCollectionCacheAsync(string userId)
    {
        try { await _cache.RemoveAsync($"collections:all:{userId}"); }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed to invalidate collection cache for user {UserId}", userId); }
    }

    /// <inheritdoc/>
    public async Task<bool> HasPermissionAsync(int collectionId, string userId, string minimumRole)
    {
        // Collection owner always has full access
        var collection = await _context.Collections.FindAsync(collectionId);
        if (collection == null) return false;
        if (collection.UserId == userId) return true;

        var perm = await _context.CollectionPermissions
            .FirstOrDefaultAsync(p => p.CollectionId == collectionId && p.UserId == userId);
        if (perm == null) return false;

        return minimumRole switch
        {
            CollectionRoles.Viewer => true,                      // any role >= viewer
            CollectionRoles.Editor => CollectionRoles.CanWrite(perm.Role),
            CollectionRoles.Owner => CollectionRoles.CanManage(perm.Role),
            _ => false
        };
    }

    /// <inheritdoc/>
    public async Task<string?> GetRoleAsync(int collectionId, string userId)
    {
        var collection = await _context.Collections.FindAsync(collectionId);
        if (collection == null) return null;
        if (collection.UserId == userId) return CollectionRoles.Owner;

        var perm = await _context.CollectionPermissions
            .FirstOrDefaultAsync(p => p.CollectionId == collectionId && p.UserId == userId);
        return perm?.Role;
    }

    /// <inheritdoc/>
    public async Task<CollectionPermission> GrantAsync(int collectionId, GrantPermissionDto dto, string grantedByUserId)
    {
        // Validate role string
        if (!CollectionRoles.All.Contains(dto.Role))
            throw new ArgumentException($"Invalid role '{dto.Role}'. Must be one of: {string.Join(", ", CollectionRoles.All)}");

        // Only owner can grant
        if (!await HasPermissionAsync(collectionId, grantedByUserId, CollectionRoles.Owner))
            throw new UnauthorizedAccessException("Only the collection owner can grant permissions.");

        // Find target user by email
        var targetUser = await _userManager.FindByEmailAsync(dto.UserEmail)
            ?? throw new KeyNotFoundException($"User with email '{dto.UserEmail}' not found.");

        // Can't grant permission to self
        if (targetUser.Id == grantedByUserId)
            throw new ArgumentException("Cannot grant permission to yourself.");

        // Check if permission already exists
        var existing = await _context.CollectionPermissions
            .FirstOrDefaultAsync(p => p.CollectionId == collectionId && p.UserId == targetUser.Id);

        if (existing != null)
        {
            existing.Role = dto.Role;
            existing.GrantedBy = grantedByUserId;
            existing.GrantedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated permission for user {TargetUserId} on collection {CollectionId} to {Role}", targetUser.Id, collectionId, dto.Role);
            await InvalidateUserCollectionCacheAsync(targetUser.Id);
            return existing;
        }

        var permission = new CollectionPermission
        {
            UserId = targetUser.Id,
            CollectionId = collectionId,
            Role = dto.Role,
            GrantedBy = grantedByUserId,
            GrantedAt = DateTime.UtcNow
        };

        _context.CollectionPermissions.Add(permission);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Granted {Role} permission to user {TargetUserId} on collection {CollectionId}", dto.Role, targetUser.Id, collectionId);
        await InvalidateUserCollectionCacheAsync(targetUser.Id);
        return permission;
    }

    /// <inheritdoc/>
    public async Task<CollectionPermission> UpdateAsync(int permissionId, UpdatePermissionDto dto, string currentUserId)
    {
        if (!CollectionRoles.All.Contains(dto.Role))
            throw new ArgumentException($"Invalid role '{dto.Role}'.");

        var permission = await _context.CollectionPermissions.FindAsync(permissionId)
            ?? throw new KeyNotFoundException($"Permission {permissionId} not found.");

        if (!await HasPermissionAsync(permission.CollectionId, currentUserId, CollectionRoles.Owner))
            throw new UnauthorizedAccessException("Only the collection owner can update permissions.");

        permission.Role = dto.Role;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated permission {PermissionId} to role {Role}", permissionId, dto.Role);
        await InvalidateUserCollectionCacheAsync(permission.UserId);
        return permission;
    }

    /// <inheritdoc/>
    public async Task<bool> RevokeAsync(int permissionId, string currentUserId)
    {
        var permission = await _context.CollectionPermissions.FindAsync(permissionId)
            ?? throw new KeyNotFoundException($"Permission {permissionId} not found.");

        if (!await HasPermissionAsync(permission.CollectionId, currentUserId, CollectionRoles.Owner))
            throw new UnauthorizedAccessException("Only the collection owner can revoke permissions.");

        _context.CollectionPermissions.Remove(permission);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Revoked permission {PermissionId} from user {UserId} on collection {CollectionId}", permissionId, permission.UserId, permission.CollectionId);
        await InvalidateUserCollectionCacheAsync(permission.UserId);
        return true;
    }

    /// <inheritdoc/>
    public async Task<List<PermissionInfoDto>> ListAsync(int collectionId, string currentUserId)
    {
        // Must have at least viewer access (or be owner) to see permissions
        if (!await HasPermissionAsync(collectionId, currentUserId, CollectionRoles.Viewer))
            throw new UnauthorizedAccessException("No access to this collection.");

        var permissions = await _context.CollectionPermissions
            .Where(p => p.CollectionId == collectionId)
            .ToListAsync();

        var result = new List<PermissionInfoDto>();
        foreach (var p in permissions)
        {
            var user = await _userManager.FindByIdAsync(p.UserId);
            result.Add(new PermissionInfoDto
            {
                Id = p.Id,
                UserId = p.UserId,
                UserEmail = user?.Email,
                DisplayName = user?.DisplayName,
                Role = p.Role,
                GrantedAt = p.GrantedAt
            });
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<List<Collection>> GetSharedCollectionsAsync(string userId)
    {
        var collectionIds = await _context.CollectionPermissions
            .Where(p => p.UserId == userId)
            .Select(p => p.CollectionId)
            .ToListAsync();

        if (collectionIds.Count == 0) return new();

        return await _context.Collections
            .Where(c => collectionIds.Contains(c.Id))
            .OrderBy(c => c.Order)
            .ToListAsync();
    }
}
