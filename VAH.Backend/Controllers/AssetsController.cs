using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

/// <summary>
/// Core asset CRUD operations + specialized asset creation.
/// SRP: Handles single-asset lifecycle and creation of typed assets.
/// Bulk operations → <see cref="BulkAssetsController"/>.
/// </summary>
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class AssetsController(IAssetService assetService) : BaseApiController
{
    // ──── Core CRUD ────

    /// <summary>Paginated list of user's assets.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<Asset>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<Asset>>> GetAssets(
        [FromQuery] PaginationParams pagination, CancellationToken ct = default)
        => Ok(await assetService.GetAssetsAsync(pagination, GetUserId(), ct));

    /// <summary>Get a single asset by ID.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Asset), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Asset>> GetAssetById(int id, CancellationToken ct = default)
        => Ok(await assetService.GetByIdAsync(id, GetUserId(), ct));

    /// <summary>Create a generic (file-type) asset from metadata.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Asset), StatusCodes.Status201Created)]
    public async Task<ActionResult<Asset>> CreateAsset(
        [FromBody] CreateAssetDto dto, CancellationToken ct = default)
    {
        var created = await assetService.CreateAssetAsync(dto, GetUserId(), ct);
        return CreatedAtAction(nameof(GetAssetById), new { id = created.Id }, created);
    }

    /// <summary>Upload one or more files to a collection.</summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(List<Asset>), StatusCodes.Status201Created)]
    public async Task<ActionResult<List<Asset>>> UploadFiles(
        List<IFormFile> files,
        [FromQuery] int collectionId = 1,
        [FromQuery] int? folderId = null,
        CancellationToken ct = default)
        => StatusCode(StatusCodes.Status201Created,
            await assetService.UploadFilesAsync(files, collectionId, folderId, GetUserId(), ct));

    /// <summary>Partial update of an asset (rename, move, regroup).</summary>
    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(Asset), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Asset>> UpdateAsset(
        int id, [FromBody] UpdateAssetDto dto, CancellationToken ct = default)
        => Ok(await assetService.UpdateAssetAsync(id, dto, GetUserId(), ct));

    /// <summary>Backward-compatible alias for PATCH (partial update).</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Asset), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ApiExplorerSettings(IgnoreApi = true)]
    public Task<ActionResult<Asset>> UpdateAssetPut(
        int id, [FromBody] UpdateAssetDto dto, CancellationToken ct = default)
        => UpdateAsset(id, dto, ct);

    /// <summary>Update canvas position (x/y coordinates).</summary>
    [HttpPut("{id}/position")]
    [ProducesResponseType(typeof(Asset), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Asset>> UpdatePosition(
        int id, [FromBody] AssetPositionDto dto, CancellationToken ct = default)
        => Ok(await assetService.UpdatePositionAsync(id, dto.PositionX, dto.PositionY, GetUserId(), ct));

    /// <summary>Delete an asset and its associated files/thumbnails.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsset(int id, CancellationToken ct = default)
    {
        await assetService.DeleteAssetAsync(id, GetUserId(), ct);
        return NoContent();
    }

    /// <summary>Duplicate (clone) an existing asset.</summary>
    [HttpPost("{id}/duplicate")]
    [ProducesResponseType(typeof(Asset), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Asset>> DuplicateAsset(
        int id, [FromBody] DuplicateAssetDto? dto = null, CancellationToken ct = default)
    {
        var clone = await assetService.DuplicateAssetAsync(id, dto?.TargetFolderId, GetUserId(), ct);
        return CreatedAtAction(nameof(GetAssetById), new { id = clone.Id }, clone);
    }

    /// <summary>Reorder assets by providing the desired ID sequence.</summary>
    [HttpPost("reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReorderAssets(
        [FromBody] ReorderAssetsDto dto, CancellationToken ct = default)
    {
        await assetService.ReorderAssetsAsync(dto.AssetIds, GetUserId(), ct);
        return NoContent();
    }

    /// <summary>Get assets belonging to a color group.</summary>
    [HttpGet("group/{groupId}")]
    [ProducesResponseType(typeof(List<Asset>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Asset>>> GetAssetsByGroup(
        int groupId, CancellationToken ct = default)
        => Ok(await assetService.GetAssetsByGroupAsync(groupId, GetUserId(), ct));

    // ──── Specialized Asset Creation (RESTful noun routes) ────

    /// <summary>Create a folder asset.</summary>
    [HttpPost("folders")]
    [ProducesResponseType(typeof(Asset), StatusCodes.Status201Created)]
    public async Task<ActionResult<Asset>> CreateFolder(
        [FromBody] CreateFolderDto dto, CancellationToken ct = default)
    {
        var folder = await assetService.CreateFolderAsync(dto, GetUserId(), ct);
        return CreatedAtAction(nameof(GetAssetById), new { id = folder.Id }, folder);
    }

    /// <summary>Create a color swatch asset.</summary>
    [HttpPost("colors")]
    [ProducesResponseType(typeof(Asset), StatusCodes.Status201Created)]
    public async Task<ActionResult<Asset>> CreateColor(
        [FromBody] CreateColorDto dto, CancellationToken ct = default)
    {
        var color = await assetService.CreateColorAsync(dto, GetUserId(), ct);
        return CreatedAtAction(nameof(GetAssetById), new { id = color.Id }, color);
    }

    /// <summary>Create a color group (palette container).</summary>
    [HttpPost("color-groups")]
    [ProducesResponseType(typeof(Asset), StatusCodes.Status201Created)]
    public async Task<ActionResult<Asset>> CreateColorGroup(
        [FromBody] CreateColorGroupDto dto, CancellationToken ct = default)
    {
        var group = await assetService.CreateColorGroupAsync(dto, GetUserId(), ct);
        return CreatedAtAction(nameof(GetAssetById), new { id = group.Id }, group);
    }

    /// <summary>Create a web link (bookmark) asset.</summary>
    [HttpPost("links")]
    [ProducesResponseType(typeof(Asset), StatusCodes.Status201Created)]
    public async Task<ActionResult<Asset>> CreateLink(
        [FromBody] CreateLinkDto dto, CancellationToken ct = default)
    {
        var link = await assetService.CreateLinkAsync(dto, GetUserId(), ct);
        return CreatedAtAction(nameof(GetAssetById), new { id = link.Id }, link);
    }
}