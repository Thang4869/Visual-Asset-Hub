using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

/// <summary>
/// Collection permission management (grant, update, revoke).
/// </summary>
[Route("api/v1/collections/{collectionId}/permissions")]
[Authorize]
[Produces("application/json")]
public class PermissionsController(IPermissionService permissionService) : BaseApiController
{
    /// <summary>List all permissions for a collection.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<PermissionInfoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<PermissionInfoDto>>> List(int collectionId, CancellationToken ct)
        => Ok(await permissionService.ListAsync(collectionId, GetUserId(), ct));

    /// <summary>Grant a permission to a user by email.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CollectionPermission), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CollectionPermission>> Grant(int collectionId, [FromBody] GrantPermissionDto dto, CancellationToken ct)
        => Ok(await permissionService.GrantAsync(collectionId, dto, GetUserId(), ct));

    /// <summary>Update an existing permission’s role.</summary>
    [HttpPut("{permissionId}")]
    [ProducesResponseType(typeof(CollectionPermission), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CollectionPermission>> Update(int collectionId, int permissionId, [FromBody] UpdatePermissionDto dto, CancellationToken ct)
        => Ok(await permissionService.UpdateAsync(permissionId, dto, GetUserId(), ct));

    /// <summary>Revoke a permission.</summary>
    [HttpDelete("{permissionId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Revoke(int collectionId, int permissionId, CancellationToken ct)
    {
        await permissionService.RevokeAsync(permissionId, GetUserId(), ct);
        return NoContent();
    }

    /// <summary>Get the current user’s role for a collection.</summary>
    [HttpGet("my-role")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyRole(int collectionId, CancellationToken ct)
    {
        var role = await permissionService.GetRoleAsync(collectionId, GetUserId(), ct);
        return Ok(new { role });
    }

    /// <summary>Get all collections shared with the current user.</summary>
    [HttpGet("/api/v1/shared-collections")]
    [ProducesResponseType(typeof(List<Collection>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Collection>>> GetSharedCollections(CancellationToken ct)
        => Ok(await permissionService.GetSharedCollectionsAsync(GetUserId(), ct));
}
