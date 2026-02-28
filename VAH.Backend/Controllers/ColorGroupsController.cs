using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

/// <summary>
/// Color group (palette) asset lifecycle — create and manage color group containers.
/// Domain: ColorGroup (palette container that holds color swatches).
/// </summary>
[Route("api/v1/assets/color-groups")]
[Produces("application/json")]
public class ColorGroupsController(IAssetService assetService) : BaseApiController
{
    /// <summary>Create a color group (palette container) asset.</summary>
    [HttpPost]
    [Authorize(Policy = "RequireAssetWrite")]
    [ProducesResponseType(typeof(AssetResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssetResponseDto>> CreateColorGroup(
        [FromBody] CreateColorGroupDto dto, CancellationToken ct = default)
    {
        var group = await assetService.CreateColorGroupAsync(dto, GetUserId(), ct);
        return CreatedAtAction(
            actionName: nameof(AssetsController.GetAssetById),
            controllerName: "Assets",
            routeValues: new { id = group.Id },
            value: group);
    }
}
