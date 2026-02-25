using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AssetsController : ControllerBase
{
    private readonly IAssetService _assetService;

    public AssetsController(IAssetService assetService)
    {
        _assetService = assetService;
    }

    // GET: api/assets?page=1&pageSize=50&sortBy=createdAt&sortOrder=desc
    [HttpGet]
    public async Task<ActionResult<PagedResult<Asset>>> GetAssets([FromQuery] PaginationParams pagination)
    {
        var result = await _assetService.GetAssetsAsync(pagination);
        return Ok(result);
    }

    // POST: api/assets
    [HttpPost]
    public async Task<ActionResult<Asset>> PostAsset(Asset asset)
    {
        var created = await _assetService.CreateAssetAsync(asset);
        return CreatedAtAction(nameof(GetAssets), new { id = created.Id }, created);
    }

    // POST: api/assets/upload?collectionId=1&folderId=2
    [HttpPost("upload")]
    public async Task<ActionResult<List<Asset>>> UploadFiles(
        List<IFormFile> files,
        [FromQuery] int collectionId = 1,
        [FromQuery] int? folderId = null)
    {
        var createdAssets = await _assetService.UploadFilesAsync(files, collectionId, folderId);
        return Ok(createdAssets);
    }

    // PUT: api/assets/{id}/position
    [HttpPut("{id}/position")]
    public async Task<ActionResult<Asset>> UpdateAssetPosition(int id, [FromBody] AssetPositionDto positionDto)
    {
        var asset = await _assetService.UpdatePositionAsync(id, positionDto.PositionX, positionDto.PositionY);
        return Ok(asset);
    }

    // POST: api/assets/create-folder
    [HttpPost("create-folder")]
    public async Task<ActionResult<Asset>> CreateFolder([FromBody] CreateFolderDto dto)
    {
        var folder = await _assetService.CreateFolderAsync(dto);
        return Ok(folder);
    }

    // POST: api/assets/create-color
    [HttpPost("create-color")]
    public async Task<ActionResult<Asset>> CreateColor([FromBody] CreateColorDto dto)
    {
        var color = await _assetService.CreateColorAsync(dto);
        return Ok(color);
    }

    // POST: api/assets/create-color-group
    [HttpPost("create-color-group")]
    public async Task<ActionResult<Asset>> CreateColorGroup([FromBody] CreateColorGroupDto dto)
    {
        var group = await _assetService.CreateColorGroupAsync(dto);
        return Ok(group);
    }

    // POST: api/assets/create-link
    [HttpPost("create-link")]
    public async Task<ActionResult<Asset>> CreateLink([FromBody] CreateLinkDto dto)
    {
        var link = await _assetService.CreateLinkAsync(dto);
        return Ok(link);
    }

    // PUT: api/assets/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<Asset>> UpdateAsset(int id, [FromBody] UpdateAssetDto dto)
    {
        var asset = await _assetService.UpdateAssetAsync(id, dto);
        return Ok(asset);
    }

    // DELETE: api/assets/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsset(int id)
    {
        await _assetService.DeleteAssetAsync(id);
        return NoContent();
    }

    // POST: api/assets/reorder
    [HttpPost("reorder")]
    public async Task<IActionResult> ReorderAssets([FromBody] ReorderAssetsDto dto)
    {
        await _assetService.ReorderAssetsAsync(dto.AssetIds);
        return Ok();
    }

    // GET: api/assets/group/{groupId}
    [HttpGet("group/{groupId}")]
    public async Task<ActionResult<List<Asset>>> GetAssetsByGroup(int groupId)
    {
        var assets = await _assetService.GetAssetsByGroupAsync(groupId);
        return Ok(assets);
    }
}

// DTO for position update
public class AssetPositionDto
{
    public double PositionX { get; set; }
    public double PositionY { get; set; }
}