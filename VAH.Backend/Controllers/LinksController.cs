using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

/// <summary>
/// Web link (bookmark) asset lifecycle — create and manage link-type assets.
/// Domain: Link (external URL bookmark).
/// </summary>
[Route("api/v1/assets/links")]
[Produces("application/json")]
public class LinksController(IAssetService assetService) : BaseApiController
{
    /// <summary>Create a web link (bookmark) asset.</summary>
    [HttpPost]
    [Authorize(Policy = "RequireAssetWrite")]
    [ProducesResponseType(typeof(AssetResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssetResponseDto>> CreateLink(
        [FromBody] CreateLinkDto dto, CancellationToken ct = default)
    {
        var link = await assetService.CreateLinkAsync(dto, GetUserId(), ct);
        return CreatedAtAction(
            actionName: nameof(AssetsController.GetAssetById),
            controllerName: "Assets",
            routeValues: new { id = link.Id },
            value: link);
    }
}
