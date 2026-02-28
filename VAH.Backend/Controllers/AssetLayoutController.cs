using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

/// <summary>
/// Canvas layout operations — position and sort-order management.
/// SRP: Spatial/ordering concerns only; lifecycle → <see cref="AssetsController"/>.
/// </summary>
[Route("api/v1/assets")]
[Produces("application/json")]
public class AssetLayoutController(IAssetService assetService) : BaseApiController
{
    /// <summary>Update canvas position (x/y coordinates).</summary>
    [HttpPut("{id}/position")]
    [Authorize(Policy = "RequireAssetWrite")]
    [ProducesResponseType(typeof(AssetResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetResponseDto>> UpdatePosition(
        [Range(1, int.MaxValue)] int id, [FromBody] AssetPositionDto dto, CancellationToken ct = default)
        => Ok(await assetService.UpdatePositionAsync(id, dto.PositionX, dto.PositionY, GetUserId(), ct));

    /// <summary>Reorder assets by providing the desired ID sequence.</summary>
    [HttpPost("reorder")]
    [Authorize(Policy = "RequireAssetWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReorderAssets(
        [FromBody] ReorderAssetsDto dto, CancellationToken ct = default)
    {
        await assetService.ReorderAssetsAsync(dto.AssetIds, GetUserId(), ct);
        return NoContent();
    }
}
