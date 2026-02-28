using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

/// <summary>
/// Folder asset lifecycle — create, manage folder-type assets.
/// Domain: Folder (organizational container within a collection).
/// </summary>
[Route("api/v1/assets/folders")]
[Produces("application/json")]
public class FoldersController(IAssetService assetService) : BaseApiController
{
    /// <summary>Create a folder asset.</summary>
    [HttpPost]
    [Authorize(Policy = "RequireAssetWrite")]
    [ProducesResponseType(typeof(AssetResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssetResponseDto>> CreateFolder(
        [FromBody] CreateFolderDto dto, CancellationToken ct = default)
    {
        var folder = await assetService.CreateFolderAsync(dto, GetUserId(), ct);
        return CreatedAtAction(
            actionName: nameof(AssetsController.GetAssetById),
            controllerName: "Assets",
            routeValues: new { id = folder.Id },
            value: folder);
    }
}
