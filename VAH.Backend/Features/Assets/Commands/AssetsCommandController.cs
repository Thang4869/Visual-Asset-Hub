using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Controllers;
using VAH.Backend.Features.Assets.Common;
using VAH.Backend.Features.Assets.Contracts;
using VAH.Backend.Features.Assets.Infrastructure.Files;
using VAH.Backend.Features.Assets.Application;
using VAH.Backend.Models;

namespace VAH.Backend.Features.Assets.Commands;

/// <summary>Command surface for assets: creates, updates, uploads, deletes, and duplicates.</summary>
/// <remarks>All mutating endpoints require <see cref="PolicyNames.RequireAssetWrite"/>.</remarks>
[Route("api/v1/assets")]
[Produces("application/json")]
public sealed class AssetsCommandController(
    IAssetApplicationService assetService,
    IFileMapperService fileMapperService) : BaseApiController
{

    /// <summary>Create a generic (file-type) asset from metadata.</summary>
    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireAssetWrite)]
    [ProducesResponseType(typeof(AssetResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssetResponseDto>> CreateAsset(
        [FromBody] CreateAssetDto dto, CancellationToken ct = default)
    {
        var created = await assetService.CreateAssetAsync(dto, ct);
        return CreatedAtRoute(AssetRouteNames.GetAssetById, new { id = created.Id }, created);
    }

    /// <summary>Upload one or more files to a collection.</summary>
    [HttpPost("upload")]
    [Authorize(Policy = PolicyNames.RequireAssetWrite)]
    [ProducesResponseType(typeof(List<AssetResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<AssetResponseDto>>> UploadFiles(
        [FromForm] UploadAssetsRequest request,
        CancellationToken ct = default)
    {
        var uploadDtos = fileMapperService.Map(request.Files);
        return StatusCode(StatusCodes.Status201Created,
            await assetService.UploadFilesAsync(uploadDtos, request.CollectionId, request.FolderId, ct));
    }

    /// <summary>Partial update of an asset (rename, move, regroup).</summary>
    [HttpPatch("{id:int}")]
    [Authorize(Policy = PolicyNames.RequireAssetWrite)]
    [ProducesResponseType(typeof(AssetResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetResponseDto>> UpdateAsset(
        [FromRoute, Range(1, int.MaxValue)] int id, [FromBody] UpdateAssetDto dto, CancellationToken ct = default)
        => Ok(await assetService.UpdateAssetAsync(id, dto, ct));

    /// <summary>Backward-compatible alias for PATCH (partial update).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = PolicyNames.RequireAssetWrite)]
    [ProducesResponseType(typeof(AssetResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ApiExplorerSettings(IgnoreApi = true)]
    public Task<ActionResult<AssetResponseDto>> UpdateAssetPut(
        [FromRoute, Range(1, int.MaxValue)] int id, [FromBody] UpdateAssetDto dto, CancellationToken ct = default)
        => UpdateAsset(id, dto, ct);

    /// <summary>Delete an asset and its associated files/thumbnails.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = PolicyNames.RequireAssetWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsset(
        [FromRoute, Range(1, int.MaxValue)] int id, CancellationToken ct = default)
    {
        await assetService.DeleteAssetAsync(id, ct);
        return NoContent();
    }

    /// <summary>Duplicate (clone) an asset and optionally target a folder.</summary>
    [HttpPost("{id:int}/duplicate")]
    [Authorize(Policy = PolicyNames.RequireAssetWrite)]
    [ProducesResponseType(typeof(AssetResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetResponseDto>> DuplicateAsset(
        [FromRoute, Range(1, int.MaxValue)] int id,
        [FromQuery] DuplicateAssetRequest request,
        CancellationToken ct = default)
    {
        var clone = await assetService.DuplicateAssetAsync(id, request.TargetFolderId, ct);
        return CreatedAtRoute(AssetRouteNames.GetAssetById, new { id = clone.Id }, clone);
    }

    /// <summary>Legacy route kept for backwards compatibility.</summary>
    [HttpPost("{id:int}/duplicate-to-folder/{folderId:int}")]
    [Authorize(Policy = PolicyNames.RequireAssetWrite)]
    [ProducesResponseType(typeof(AssetResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ApiExplorerSettings(IgnoreApi = true)]
    public Task<ActionResult<AssetResponseDto>> DuplicateAssetToFolder(
        [FromRoute, Range(1, int.MaxValue)] int id,
        [FromRoute, Range(1, int.MaxValue)] int folderId,
        CancellationToken ct = default)
        => DuplicateAsset(id, new DuplicateAssetRequest { TargetFolderId = folderId }, ct);
}
