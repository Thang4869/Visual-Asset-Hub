using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Features.Assets.Common;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

/// <summary>Web link (bookmark) asset lifecycle — create and manage link-type assets.</summary>
/// <remarks>Domain: Link (external URL bookmark).</remarks>
[Route("api/v1/assets/links")]
[Produces("application/json")]
public sealed class LinksController(IAssetService assetService) : BaseApiController
{
    /// <summary>Create a web link (bookmark) asset.</summary>
    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireAssetWrite)]
    [ProducesResponseType(typeof(AssetResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssetResponseDto>> CreateLink(
        [FromBody] CreateLinkDto dto, CancellationToken ct = default)
    {
        var link = await assetService.CreateLinkAsync(dto, GetUserId(), ct);
        return CreatedAtRoute(
            routeName: AssetRouteNames.GetAssetById,
            routeValues: new { id = link.Id },
            value: link);
    }
}
