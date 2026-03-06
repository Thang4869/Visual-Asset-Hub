using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Features.Assets.Common;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

/// <summary>Web link (bookmark) asset lifecycle — create and manage link-type assets.</summary>
/// <remarks>
/// Domain: Link (external URL bookmark).
/// <para>Separated per asset type for independent evolution. See <see cref="ColorsController"/> remarks.</para>
/// </remarks>
[Route("api/v1/assets/links")]
[Produces("application/json")]
public sealed class LinksController(
    IAssetService assetService,
    ILogger<LinksController> logger) : BaseApiController
{
    /// <summary>Create a web link (bookmark) asset.</summary>
    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireAssetWrite)]
    [ProducesResponseType(typeof(AssetResponseDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AssetResponseDto>> CreateLink(
        [FromBody] CreateLinkDto dto, CancellationToken ct = default)
    {
        var userId = GetUserId();
        logger.LogInformation("Creating link asset '{Name}' in collection {CollectionId} by user {UserId}",
            dto.Name, dto.CollectionId, userId);
        var link = await assetService.CreateLinkAsync(dto, userId, ct);
        return CreatedAtRoute(
            routeName: AssetRouteNames.GetAssetById,
            routeValues: new { id = link.Id },
            value: link);
    }
}
