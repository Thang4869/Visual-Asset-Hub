using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Controllers.Filters;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

/// <summary>Canvas layout operations — position and sort-order management.</summary>
/// <remarks>
/// SRP: Spatial/ordering concerns only; lifecycle → <see cref="Features.Assets.Commands.AssetsCommandController"/>.
/// <para>All mutating endpoints require <see cref="PolicyNames.RequireAssetWrite"/>.</para>
/// </remarks>
[Route("api/v1/assets")]
[Produces("application/json")]
public sealed class AssetLayoutController(
    IAssetService assetService,
    ILogger<AssetLayoutController> logger) : BaseApiController
{
    /// <summary>Update canvas position (x/y coordinates).</summary>
    [HttpPut("{id:int}/position")]
    [Authorize(Policy = PolicyNames.RequireAssetWrite)]
    [ProducesResponseType(typeof(AssetResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetResponseDto>> UpdatePosition(
        [FromRoute, Range(1, int.MaxValue)] int id,
        [FromBody] AssetPositionDto dto,
        CancellationToken ct = default)
        => Ok(await assetService.UpdatePositionAsync(id, dto.PositionX, dto.PositionY, GetUserId(), ct));

    /// <summary>Reorder assets by providing the desired ID sequence.</summary>
    [HttpPost("reorder")]
    [Authorize(Policy = PolicyNames.RequireAssetWrite)]
    [ValidateBatchFilter]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReorderAssets(
        [FromBody] ReorderAssetsDto dto,
        CancellationToken ct = default)
    {
        var userId = GetUserId();
        logger.LogInformation(LogEvents.Reorder, "Reorder requested for {Count} assets by {UserId}",
            dto.AssetIds.Count, userId);
        await assetService.ReorderAssetsAsync(dto.AssetIds, userId, ct);
        return NoContent();
    }
}
