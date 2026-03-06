using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Features.Assets.Common;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

/// <summary>Color swatch asset lifecycle — create and manage individual color assets.</summary>
/// <remarks>
/// Domain: Color (single swatch within a palette/group).
/// <para>Intentionally separated per asset type for independent evolution:
/// each type may gain type-specific endpoints (e.g., palette extraction, color validation)
/// without bloating a generic controller. Shared patterns are centralized in
/// <see cref="BaseApiController"/> and route naming via <see cref="Features.Assets.Common.AssetRouteNames"/>.</para>
/// </remarks>
[Route("api/v1/assets/colors")]
[Produces("application/json")]
public sealed class ColorsController(
    IAssetService assetService,
    ILogger<ColorsController> logger) : BaseApiController
{
    /// <summary>Create a color swatch asset.</summary>
    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireAssetWrite)]
    [ProducesResponseType(typeof(AssetResponseDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AssetResponseDto>> CreateColor(
        [FromBody] CreateColorDto dto, CancellationToken ct = default)
    {
        var userId = GetUserId();
        logger.LogInformation("Creating color asset '{ColorCode}' in collection {CollectionId} by user {UserId}",
            dto.ColorCode, dto.CollectionId, userId);
        var color = await assetService.CreateColorAsync(dto, userId, ct);
        return CreatedAtRoute(
            routeName: AssetRouteNames.GetAssetById,
            routeValues: new { id = color.Id },
            value: color);
    }
}
