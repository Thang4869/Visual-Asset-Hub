using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.CQRS.Assets.Commands;
using VAH.Backend.CQRS.Assets.Queries;
using VAH.Backend.Models;

namespace VAH.Backend.Controllers;

/// <summary>
/// Core asset lifecycle — CRUD, upload, and duplicate.
/// CQRS: Dispatches Commands (writes) and Queries (reads) via MediatR.
/// Domain-specific creation → <see cref="FoldersController"/>, <see cref="ColorsController"/>,
/// <see cref="ColorGroupsController"/>, <see cref="LinksController"/>.
/// Layout (position/reorder) → <see cref="AssetLayoutController"/>.
/// Bulk operations → <see cref="BulkAssetsController"/>.
/// </summary>
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AssetsController(ISender sender) : BaseApiController
{
    // ──── Queries (Reads) ────

    /// <summary>Paginated list of user's assets.</summary>
    [HttpGet]
    [Authorize(Policy = "RequireAssetRead")]
    [ProducesResponseType(typeof(PagedResult<AssetResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AssetResponseDto>>> GetAssets(
        [FromQuery] PaginationParams pagination, CancellationToken ct = default)
        => Ok(await sender.Send(new GetAssetsQuery(pagination, GetUserId()), ct));

    /// <summary>Get a single asset by ID.</summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "RequireAssetRead")]
    [ProducesResponseType(typeof(AssetResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetResponseDto>> GetAssetById(
        [Range(1, int.MaxValue)] int id, CancellationToken ct = default)
        => Ok(await sender.Send(new GetAssetByIdQuery(id, GetUserId()), ct));

    /// <summary>Get assets belonging to a color group.</summary>
    [HttpGet("group/{groupId}")]
    [Authorize(Policy = "RequireAssetRead")]
    [ProducesResponseType(typeof(List<AssetResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AssetResponseDto>>> GetAssetsByGroup(
        [Range(1, int.MaxValue)] int groupId, CancellationToken ct = default)
        => Ok(await sender.Send(new GetAssetsByGroupQuery(groupId, GetUserId()), ct));

    // ──── Commands (Writes) ────

    /// <summary>Create a generic (file-type) asset from metadata.</summary>
    [HttpPost]
    [Authorize(Policy = "RequireAssetWrite")]
    [ProducesResponseType(typeof(AssetResponseDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AssetResponseDto>> CreateAsset(
        [FromBody] CreateAssetDto dto, CancellationToken ct = default)
    {
        var created = await sender.Send(new CreateAssetCommand(dto, GetUserId()), ct);
        return CreatedAtAction(nameof(GetAssetById), new { id = created.Id }, created);
    }

    /// <summary>Upload one or more files to a collection.</summary>
    [HttpPost("upload")]
    [Authorize(Policy = "RequireAssetWrite")]
    [ProducesResponseType(typeof(List<AssetResponseDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<List<AssetResponseDto>>> UploadFiles(
        [FromForm] List<IFormFile> files,
        [FromQuery] int collectionId = 1,
        [FromQuery] int? folderId = null,
        CancellationToken ct = default)
        => StatusCode(StatusCodes.Status201Created,
            await sender.Send(new UploadFilesCommand(files, collectionId, folderId, GetUserId()), ct));

    /// <summary>Partial update of an asset (rename, move, regroup).</summary>
    [HttpPatch("{id}")]
    [Authorize(Policy = "RequireAssetWrite")]
    [ProducesResponseType(typeof(AssetResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetResponseDto>> UpdateAsset(
        [Range(1, int.MaxValue)] int id, [FromBody] UpdateAssetDto dto, CancellationToken ct = default)
        => Ok(await sender.Send(new UpdateAssetCommand(id, dto, GetUserId()), ct));

    /// <summary>Backward-compatible alias for PATCH (partial update).</summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "RequireAssetWrite")]
    [ProducesResponseType(typeof(AssetResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ApiExplorerSettings(IgnoreApi = true)]
    public Task<ActionResult<AssetResponseDto>> UpdateAssetPut(
        [Range(1, int.MaxValue)] int id, [FromBody] UpdateAssetDto dto, CancellationToken ct = default)
        => UpdateAsset(id, dto, ct);

    /// <summary>Delete an asset and its associated files/thumbnails.</summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireAssetWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsset(
        [Range(1, int.MaxValue)] int id, CancellationToken ct = default)
    {
        await sender.Send(new DeleteAssetCommand(id, GetUserId()), ct);
        return NoContent();
    }

    /// <summary>Duplicate (clone) an asset in-place (same folder).</summary>
    [HttpPost("{id}/duplicate")]
    [Authorize(Policy = "RequireAssetWrite")]
    [ProducesResponseType(typeof(AssetResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetResponseDto>> DuplicateAsset(
        [Range(1, int.MaxValue)] int id, CancellationToken ct = default)
    {
        var clone = await sender.Send(new DuplicateAssetCommand(id, null, GetUserId()), ct);
        return CreatedAtAction(nameof(GetAssetById), new { id = clone.Id }, clone);
    }

    /// <summary>Duplicate (clone) an asset into a specific target folder.</summary>
    [HttpPost("{id}/duplicate-to-folder/{folderId}")]
    [Authorize(Policy = "RequireAssetWrite")]
    [ProducesResponseType(typeof(AssetResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetResponseDto>> DuplicateAssetToFolder(
        [Range(1, int.MaxValue)] int id,
        [Range(1, int.MaxValue)] int folderId,
        CancellationToken ct = default)
    {
        var clone = await sender.Send(new DuplicateAssetCommand(id, folderId, GetUserId()), ct);
        return CreatedAtAction(nameof(GetAssetById), new { id = clone.Id }, clone);
    }
}