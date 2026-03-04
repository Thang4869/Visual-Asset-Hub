using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Features.Assets.Common;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

/// <summary>Folder asset lifecycle — create, manage folder-type assets.</summary>
/// <remarks>Domain: Folder (organizational container within a collection).</remarks>
[Route("api/v1/assets/folders")]
[Produces("application/json")]
public sealed class FoldersController(IAssetService assetService) : BaseApiController
{
    /// <summary>Create a folder asset.</summary>
    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireAssetWrite)]
    [ProducesResponseType(typeof(AssetResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssetResponseDto>> CreateFolder(
        [FromBody] CreateFolderDto dto, CancellationToken ct = default)
    {
        var folder = await assetService.CreateFolderAsync(dto, GetUserId(), ct);
        return CreatedAtRoute(
            routeName: AssetRouteNames.GetAssetById,
            routeValues: new { id = folder.Id },
            value: folder);
    }
}
