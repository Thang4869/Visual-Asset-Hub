using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

[ApiController]
[Route("api/collections/{collectionId}/permissions")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;

    public PermissionsController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>List all permissions for a collection.</summary>
    [HttpGet]
    public async Task<IActionResult> List(int collectionId)
    {
        var permissions = await _permissionService.ListAsync(collectionId, UserId);
        return Ok(permissions);
    }

    /// <summary>Grant a permission to a user by email.</summary>
    [HttpPost]
    public async Task<IActionResult> Grant(int collectionId, [FromBody] GrantPermissionDto dto)
    {
        var permission = await _permissionService.GrantAsync(collectionId, dto, UserId);
        return Ok(permission);
    }

    /// <summary>Update an existing permission's role.</summary>
    [HttpPut("{permissionId}")]
    public async Task<IActionResult> Update(int collectionId, int permissionId, [FromBody] UpdatePermissionDto dto)
    {
        var permission = await _permissionService.UpdateAsync(permissionId, dto, UserId);
        return Ok(permission);
    }

    /// <summary>Revoke a permission.</summary>
    [HttpDelete("{permissionId}")]
    public async Task<IActionResult> Revoke(int collectionId, int permissionId)
    {
        await _permissionService.RevokeAsync(permissionId, UserId);
        return NoContent();
    }

    /// <summary>Get the current user's role for a collection.</summary>
    [HttpGet("my-role")]
    public async Task<IActionResult> GetMyRole(int collectionId)
    {
        var role = await _permissionService.GetRoleAsync(collectionId, UserId);
        return Ok(new { role });
    }

    /// <summary>Get all collections shared with the current user.</summary>
    [HttpGet("/api/shared-collections")]
    public async Task<IActionResult> GetSharedCollections()
    {
        var collections = await _permissionService.GetSharedCollectionsAsync(UserId);
        return Ok(collections);
    }
}
