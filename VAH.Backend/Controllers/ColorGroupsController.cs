using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Features.Assets.Common;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

/// <summary>Color group (palette) asset lifecycle — create and manage color group containers.</summary>
/// <remarks>
/// Domain: ColorGroup (palette container that holds color swatches).
/// <para>Separated per asset type for independent evolution. See <see cref="ColorsController"/> remarks.</para>
/// </remarks>
[Route("api/v1/assets/color-groups")]
[Produces("application/json")]
public sealed class ColorGroupsController(
    IAssetService assetService,
    ILogger<ColorGroupsController> logger) : BaseApiController
{
    /// <summary>Create a color group (palette container) asset.</summary>
    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireAssetWrite)]
    [ProducesResponseType(typeof(AssetResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssetResponseDto>> CreateColorGroup(
        [FromBody] CreateColorGroupDto dto, CancellationToken ct = default)
    {
        var userId = GetUserId();
        logger.LogInformation(LogEvents.AssetCreated, "Creating color group '{GroupName}' in collection {CollectionId} by user {UserId}",
            dto.GroupName, dto.CollectionId, userId);
        var group = await assetService.CreateColorGroupAsync(dto, userId, ct);
        return CreatedAtRoute(
            routeName: AssetRouteNames.GetAssetById,
            routeValues: new { id = group.Id },
            value: group);
    }
}
