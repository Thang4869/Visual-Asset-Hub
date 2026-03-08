using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

/// <summary>Collection permission management (grant, update, revoke).</summary>
/// <remarks>
/// All endpoints scoped to <c>collectionId</c>. Authorization enforced in service layer
/// (owner-only for mutations). Service-layer verifies permission.CollectionId
/// for Update/Revoke as defense-in-depth, independent of the route parameter.
/// User-scoped queries (shared-collections) live in <see cref="SharedCollectionsController"/>.
/// </remarks>
[Route("api/v1/collections/{collectionId:int}/permissions")]
[Authorize]
[Produces("application/json")]
public sealed class PermissionsController(
    IPermissionService permissionService,
    ILogger<PermissionsController> logger) : BaseApiController
{
    /// <summary>List all permissions for a collection.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<PermissionInfoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<PermissionInfoDto>>> List(
        [FromRoute] int collectionId, CancellationToken ct = default)
        => Ok(await permissionService.ListAsync(collectionId, GetUserId(), ct));

    /// <summary>Grant a permission to a user by email.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CollectionPermission), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CollectionPermission>> Grant(
        [FromRoute] int collectionId, [FromBody] GrantPermissionDto dto, CancellationToken ct = default)
    {
        var userId = GetUserId();
        logger.LogInformation(LogEvents.PermissionGranted,
            "Granting permission on collection {CollectionId} by user {UserId}",
            collectionId, userId);
        return Ok(await permissionService.GrantAsync(collectionId, dto, userId, ct));
    }

    /// <summary>Update an existing permission’s role.</summary>
    [HttpPut("{permissionId:int}")]
    [ProducesResponseType(typeof(CollectionPermission), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CollectionPermission>> Update(
        [FromRoute] int collectionId, [FromRoute] int permissionId,
        [FromBody] UpdatePermissionDto dto, CancellationToken ct = default)
    {
        var userId = GetUserId();
        logger.LogInformation(LogEvents.PermissionUpdated,
            "Updating permission {PermissionId} on collection {CollectionId} by user {UserId}",
            permissionId, collectionId, userId);
        return Ok(await permissionService.UpdateAsync(permissionId, dto, userId, ct));
    }

    /// <summary>Revoke a permission.</summary>
    [HttpDelete("{permissionId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Revoke(
        [FromRoute] int collectionId, [FromRoute] int permissionId, CancellationToken ct = default)
    {
        var userId = GetUserId();
        logger.LogInformation(LogEvents.PermissionRevoked,
            "Revoking permission {PermissionId} on collection {CollectionId} by user {UserId}",
            permissionId, collectionId, userId);
        await permissionService.RevokeAsync(permissionId, userId, ct);
        return NoContent();
    }

    /// <summary>Get the current user’s role for a collection.</summary>
    [HttpGet("my-role")]
    [ProducesResponseType(typeof(RoleResult), StatusCodes.Status200OK)]    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]    public async Task<ActionResult<RoleResult>> GetMyRole(
        [FromRoute] int collectionId, CancellationToken ct = default)
    {
        var role = await permissionService.GetRoleAsync(collectionId, GetUserId(), ct);
        return Ok(new RoleResult(role));
    }
}
