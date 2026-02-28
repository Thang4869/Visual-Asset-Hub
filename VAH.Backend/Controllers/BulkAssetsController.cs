using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

/// <summary>
/// Bulk operations on assets (delete, move, tag, group).
/// SRP: Separated from AssetsController because bulk operations have
/// different performance characteristics, authorization patterns, and error semantics.
/// Route: api/assets/bulk-* for backward compatibility with frontend.
/// </summary>
[Route("api/v1/assets")]
[Authorize(Policy = "RequireAssetWrite")]
[Produces("application/json")]
public class BulkAssetsController(IBulkAssetService bulkService) : BaseApiController
{
    /// <summary>Delete multiple assets at once.</summary>
    [HttpPost("bulk-delete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> BulkDelete(
        [FromBody] BulkDeleteDto dto, CancellationToken ct = default)
    {
        var count = await bulkService.BulkDeleteAsync(dto.AssetIds, GetUserId(), ct);
        return Ok(new { deleted = count });
    }

    /// <summary>Move multiple assets to a different collection/folder.</summary>
    [HttpPost("bulk-move")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> BulkMove(
        [FromBody] BulkMoveDto dto, CancellationToken ct = default)
    {
        var count = await bulkService.BulkMoveAsync(dto, GetUserId(), ct);
        return Ok(new { moved = count });
    }

    /// <summary>Move multiple colors between groups with positional insert.</summary>
    [HttpPost("bulk-move-group")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> BulkMoveGroup(
        [FromBody] BulkMoveGroupDto dto, CancellationToken ct = default)
    {
        var count = await bulkService.BulkMoveGroupAsync(dto, GetUserId(), ct);
        return Ok(new { moved = count });
    }

    /// <summary>Add or remove tags on multiple assets.</summary>
    [HttpPost("bulk-tag")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> BulkTag(
        [FromBody] BulkTagDto dto, CancellationToken ct = default)
    {
        var count = await bulkService.BulkTagAsync(dto, GetUserId(), ct);
        return Ok(new { affected = count });
    }
}
