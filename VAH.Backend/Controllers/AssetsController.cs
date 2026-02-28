using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

[Route("api/[controller]")]
[Authorize]
public class AssetsController : BaseApiController
{
    private readonly IAssetService _assetService;
    private readonly IBulkAssetService _bulkService;

    public AssetsController(IAssetService assetService, IBulkAssetService bulkService)
    {
        _assetService = assetService;
        _bulkService = bulkService;
    }

    // GET: api/assets?page=1&pageSize=50&sortBy=createdAt&sortOrder=desc
    [HttpGet]
    public async Task<ActionResult<PagedResult<Asset>>> GetAssets([FromQuery] PaginationParams pagination)
    {
        var result = await _assetService.GetAssetsAsync(pagination, GetUserId());
        return Ok(result);
    }

    // POST: api/assets
    [HttpPost]
    public async Task<ActionResult<Asset>> PostAsset(Asset asset)
    {
        var created = await _assetService.CreateAssetAsync(asset, GetUserId());
        return CreatedAtAction(nameof(GetAssets), new { id = created.Id }, created);
    }

    // POST: api/assets/upload?collectionId=1&folderId=2
    [HttpPost("upload")]
    public async Task<ActionResult<List<Asset>>> UploadFiles(
        List<IFormFile> files,
        [FromQuery] int collectionId = 1,
        [FromQuery] int? folderId = null)
    {
        var createdAssets = await _assetService.UploadFilesAsync(files, collectionId, folderId, GetUserId());
        return Ok(createdAssets);
    }

    // PUT: api/assets/{id}/position
    [HttpPut("{id}/position")]
    public async Task<ActionResult<Asset>> UpdateAssetPosition(int id, [FromBody] AssetPositionDto positionDto)
    {
        var asset = await _assetService.UpdatePositionAsync(id, positionDto.PositionX, positionDto.PositionY, GetUserId());
        return Ok(asset);
    }

    // POST: api/assets/create-folder
    [HttpPost("create-folder")]
    public async Task<ActionResult<Asset>> CreateFolder([FromBody] CreateFolderDto dto)
    {
        var folder = await _assetService.CreateFolderAsync(dto, GetUserId());
        return Ok(folder);
    }

    // POST: api/assets/create-color
    [HttpPost("create-color")]
    public async Task<ActionResult<Asset>> CreateColor([FromBody] CreateColorDto dto)
    {
        var color = await _assetService.CreateColorAsync(dto, GetUserId());
        return Ok(color);
    }

    // POST: api/assets/create-color-group
    [HttpPost("create-color-group")]
    public async Task<ActionResult<Asset>> CreateColorGroup([FromBody] CreateColorGroupDto dto)
    {
        var group = await _assetService.CreateColorGroupAsync(dto, GetUserId());
        return Ok(group);
    }

    // POST: api/assets/create-link
    [HttpPost("create-link")]
    public async Task<ActionResult<Asset>> CreateLink([FromBody] CreateLinkDto dto)
    {
        var link = await _assetService.CreateLinkAsync(dto, GetUserId());
        return Ok(link);
    }

    // PUT: api/assets/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<Asset>> UpdateAsset(int id, [FromBody] UpdateAssetDto dto)
    {
        var asset = await _assetService.UpdateAssetAsync(id, dto, GetUserId());
        return Ok(asset);
    }

    // DELETE: api/assets/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsset(int id)
    {
        await _assetService.DeleteAssetAsync(id, GetUserId());
        return NoContent();
    }

    // POST: api/assets/reorder
    [HttpPost("reorder")]
    public async Task<IActionResult> ReorderAssets([FromBody] ReorderAssetsDto dto)
    {
        await _assetService.ReorderAssetsAsync(dto.AssetIds, GetUserId());
        return Ok();
    }

    // GET: api/assets/group/{groupId}
    [HttpGet("group/{groupId}")]
    public async Task<ActionResult<List<Asset>>> GetAssetsByGroup(int groupId)
    {
        var assets = await _assetService.GetAssetsByGroupAsync(groupId, GetUserId());
        return Ok(assets);
    }

    // ──── Bulk Operations ────

    // POST: api/assets/bulk-delete
    [HttpPost("bulk-delete")]
    public async Task<ActionResult> BulkDelete([FromBody] BulkDeleteDto dto)
    {
        var count = await _bulkService.BulkDeleteAsync(dto.AssetIds, GetUserId());
        return Ok(new { deleted = count });
    }

    // POST: api/assets/bulk-move
    [HttpPost("bulk-move")]
    public async Task<ActionResult> BulkMove([FromBody] BulkMoveDto dto)
    {
        var count = await _bulkService.BulkMoveAsync(dto, GetUserId());
        return Ok(new { moved = count });
    }

    // POST: api/assets/bulk-move-group
    [HttpPost("bulk-move-group")]
    public async Task<ActionResult> BulkMoveGroup([FromBody] BulkMoveGroupDto dto)
    {
        var count = await _bulkService.BulkMoveGroupAsync(dto, GetUserId());
        return Ok(new { moved = count });
    }

    // POST: api/assets/bulk-tag
    [HttpPost("bulk-tag")]
    public async Task<ActionResult> BulkTag([FromBody] BulkTagDto dto)
    {
        var count = await _bulkService.BulkTagAsync(dto, GetUserId());
        return Ok(new { affected = count });
    }
}