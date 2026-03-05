using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

/// <summary>Bulk operations on assets (delete, move, tag, group).</summary>
/// <remarks>
/// SRP: Separated from AssetsController because bulk operations have
/// different performance characteristics, authorization patterns, and error semantics.
/// <para>Rate-limited to prevent abuse of expensive batch operations.</para>
/// </remarks>
[Route("api/v1/assets")]
[Authorize(Policy = PolicyNames.RequireAssetWrite)]
[EnableRateLimiting(RateLimitPolicies.Fixed)]
[Produces("application/json")]
public sealed class BulkAssetsController(
    IBulkAssetService bulkService,
    ILogger<BulkAssetsController> logger) : BaseApiController
{
    /// <summary>Delete multiple assets at once.</summary>
    [HttpPost("bulk-delete")]
    [ProducesResponseType(typeof(BulkDeleteResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<BulkDeleteResult>> BulkDelete(
        [FromBody] BulkDeleteDto dto, CancellationToken ct = default)
    {
        if (dto.AssetIds is not { Count: > 0 })
            return BadRequest(new ProblemDetails { Title = "AssetIds must not be empty.", Status = 400 });

        var userId = GetUserId();
        logger.LogInformation("Bulk delete requested for {Count} assets by {UserId}",
            dto.AssetIds.Count, userId);
        var count = await bulkService.BulkDeleteAsync(dto.AssetIds, userId, ct);
        return Ok(new BulkDeleteResult(count));
    }

    /// <summary>Move multiple assets to a different collection/folder.</summary>
    [HttpPost("bulk-move")]
    [ProducesResponseType(typeof(BulkMoveResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<BulkMoveResult>> BulkMove(
        [FromBody] BulkMoveDto dto, CancellationToken ct = default)
    {
        if (dto.AssetIds is not { Count: > 0 })
            return BadRequest(new ProblemDetails { Title = "AssetIds must not be empty.", Status = 400 });

        var userId = GetUserId();
        logger.LogInformation("Bulk move requested for {Count} assets by {UserId}",
            dto.AssetIds.Count, userId);
        var count = await bulkService.BulkMoveAsync(dto, userId, ct);
        return Ok(new BulkMoveResult(count));
    }

    /// <summary>Move multiple colors between groups with positional insert.</summary>
    [HttpPost("bulk-move-group")]
    [ProducesResponseType(typeof(BulkMoveResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<BulkMoveResult>> BulkMoveGroup(
        [FromBody] BulkMoveGroupDto dto, CancellationToken ct = default)
    {
        if (dto.AssetIds is not { Count: > 0 })
            return BadRequest(new ProblemDetails { Title = "AssetIds must not be empty.", Status = 400 });

        var userId = GetUserId();
        logger.LogInformation("Bulk move-group requested for {Count} assets by {UserId}",
            dto.AssetIds.Count, userId);
        var count = await bulkService.BulkMoveGroupAsync(dto, userId, ct);
        return Ok(new BulkMoveResult(count));
    }

    /// <summary>Add or remove tags on multiple assets.</summary>
    [HttpPost("bulk-tag")]
    [ProducesResponseType(typeof(BulkTagResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<BulkTagResult>> BulkTag(
        [FromBody] BulkTagDto dto, CancellationToken ct = default)
    {
        if (dto.AssetIds is not { Count: > 0 })
            return BadRequest(new ProblemDetails { Title = "AssetIds must not be empty.", Status = 400 });

        var userId = GetUserId();
        logger.LogInformation("Bulk tag requested for {Count} assets by {UserId}",
            dto.AssetIds.Count, userId);
        var count = await bulkService.BulkTagAsync(dto, userId, ct);
        return Ok(new BulkTagResult(count));
    }
}
