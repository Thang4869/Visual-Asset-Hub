using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Features.Assets.Application;
using VAH.Backend.Controllers;
using VAH.Backend.Features.Assets.Common;
using VAH.Backend.Models;

namespace VAH.Backend.Features.Assets.Queries;

/// <summary>Query surface for assets: read-only endpoints and pagination helpers.</summary>
/// <remarks>All endpoints require <see cref="PolicyNames.RequireAssetRead"/>.</remarks>
[Route("api/v1/assets")]
[Produces("application/json")]
public sealed class AssetsQueryController(IAssetApplicationService assetService) : BaseApiController
{
    /// <summary>Paginated list of user's assets.</summary>
    [HttpGet(Name = "GetAssets")]
    [Authorize(Policy = PolicyNames.RequireAssetRead)]
    [ProducesResponseType(typeof(PagedResult<AssetResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AssetResponseDto>>> GetAssets(
        [FromQuery] PaginationParams pagination, CancellationToken ct = default)
        => Ok(await assetService.GetAssetsAsync(pagination, ct));

    /// <summary>Get a single asset by ID.</summary>
    [HttpGet("{id:int}", Name = AssetRouteNames.GetAssetById)]
    [Authorize(Policy = PolicyNames.RequireAssetRead)]
    [ProducesResponseType(typeof(AssetResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetResponseDto>> GetAssetById(
        [FromRoute, Range(1, int.MaxValue)] int id, CancellationToken ct = default)
        => Ok(await assetService.GetAssetByIdAsync(id, ct));

    /// <summary>Get assets belonging to a color group.</summary>
    [HttpGet("group/{groupId:int}")]
    [Authorize(Policy = PolicyNames.RequireAssetRead)]
    [ProducesResponseType(typeof(List<AssetResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AssetResponseDto>>> GetAssetsByGroup(
        [FromRoute, Range(1, int.MaxValue)] int groupId, CancellationToken ct = default)
        => Ok(await assetService.GetAssetsByGroupAsync(groupId, ct));
}
