using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

/// <summary>Shared collections — user-scoped queries for collections shared with the current user.</summary>
/// <remarks>
/// Extracted from <see cref="PermissionsController"/> to enforce single-responsibility:
/// PermissionsController handles collection-scoped permission CRUD,
/// this controller handles user-scoped shared-collection queries.
/// </remarks>
[Route("api/v1/shared-collections")]
[Authorize]
[Produces("application/json")]
public sealed class SharedCollectionsController(
    IPermissionService permissionService) : BaseApiController
{
    /// <summary>Get all collections shared with the current user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Collection>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Collection>>> GetSharedCollections(CancellationToken ct = default)
        => Ok(await permissionService.GetSharedCollectionsAsync(GetUserId(), ct));
}
