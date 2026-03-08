using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Features.Assets.Common;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

/// <summary>Folder asset lifecycle — create, manage folder-type assets.</summary>
/// <remarks>
/// Domain: Folder (organizational container within a collection).
/// <para>Separated per asset type for independent evolution. See <see cref="ColorsController"/> remarks.</para>
/// </remarks>
[Route("api/v1/assets/folders")]
[Produces("application/json")]
public sealed class FoldersController(
    IAssetService assetService,
    ILogger<FoldersController> logger) : BaseApiController
{
    /// <summary>Create a folder asset.</summary>
    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireAssetWrite)]
    [ProducesResponseType(typeof(AssetResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssetResponseDto>> CreateFolder(
        [FromBody] CreateFolderDto dto, CancellationToken ct = default)
    {
        var userId = GetUserId();
        logger.LogInformation(LogEvents.AssetCreated, "Creating folder '{FolderName}' in collection {CollectionId} by user {UserId}",
            dto.FolderName, dto.CollectionId, userId);
        var folder = await assetService.CreateFolderAsync(dto, userId, ct);
        return CreatedAtRoute(
            routeName: AssetRouteNames.GetAssetById,
            routeValues: new { id = folder.Id },
            value: folder);
    }
}
