using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Features.Assets.Common;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

/// <summary>Color group (palette) asset lifecycle — create and manage color group containers.</summary>
/// <remarks>Domain: ColorGroup (palette container that holds color swatches).</remarks>
[Route("api/v1/assets/color-groups")]
[Produces("application/json")]
public sealed class ColorGroupsController(IAssetService assetService) : BaseApiController
{
    /// <summary>Create a color group (palette container) asset.</summary>
    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireAssetWrite)]
    [ProducesResponseType(typeof(AssetResponseDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AssetResponseDto>> CreateColorGroup(
        [FromBody] CreateColorGroupDto dto, CancellationToken ct = default)
    {
        var group = await assetService.CreateColorGroupAsync(dto, GetUserId(), ct);
        return CreatedAtRoute(
            routeName: AssetRouteNames.GetAssetById,
            routeValues: new { id = group.Id },
            value: group);
    }
}
