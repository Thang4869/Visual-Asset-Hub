using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Features.Assets.Common;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

/// <summary>
/// Color swatch asset lifecycle — create and manage individual color assets.
/// Domain: Color (single swatch within a palette/group).
/// </summary>
[Route("api/v1/assets/colors")]
[Produces("application/json")]
public class ColorsController(IAssetService assetService) : BaseApiController
{
    /// <summary>Create a color swatch asset.</summary>
    [HttpPost]
    [Authorize(Policy = "RequireAssetWrite")]
    [ProducesResponseType(typeof(AssetResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssetResponseDto>> CreateColor(
        [FromBody] CreateColorDto dto, CancellationToken ct = default)
    {
        var color = await assetService.CreateColorAsync(dto, GetUserId(), ct);
        return CreatedAtRoute(
            routeName: AssetRouteNames.GetAssetById,
            routeValues: new { id = color.Id },
            value: color);
    }
}
