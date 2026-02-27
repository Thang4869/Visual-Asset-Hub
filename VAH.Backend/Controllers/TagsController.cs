using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

[Route("api/[controller]")]
[Authorize]
public class TagsController : BaseApiController
{
    private readonly ITagService _tagService;

    public TagsController(ITagService tagService)
    {
        _tagService = tagService;
    }

    // GET: api/tags
    [HttpGet]
    public async Task<ActionResult<List<Tag>>> GetTags()
    {
        var tags = await _tagService.GetAllAsync(GetUserId());
        return Ok(tags);
    }

    // GET: api/tags/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Tag>> GetTag(int id)
    {
        var tag = await _tagService.GetByIdAsync(id, GetUserId());
        return Ok(tag);
    }

    // POST: api/tags
    [HttpPost]
    public async Task<ActionResult<Tag>> CreateTag([FromBody] CreateTagDto dto)
    {
        var tag = await _tagService.CreateAsync(dto, GetUserId());
        return CreatedAtAction(nameof(GetTag), new { id = tag.Id }, tag);
    }

    // PUT: api/tags/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<Tag>> UpdateTag(int id, [FromBody] UpdateTagDto dto)
    {
        var tag = await _tagService.UpdateAsync(id, dto, GetUserId());
        return Ok(tag);
    }

    // DELETE: api/tags/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTag(int id)
    {
        await _tagService.DeleteAsync(id, GetUserId());
        return NoContent();
    }

    // GET: api/tags/asset/{assetId}
    [HttpGet("asset/{assetId}")]
    public async Task<ActionResult<List<Tag>>> GetAssetTags(int assetId)
    {
        var tags = await _tagService.GetAssetTagsAsync(assetId, GetUserId());
        return Ok(tags);
    }

    // PUT: api/tags/asset/{assetId}  — Replace all tags on an asset
    [HttpPut("asset/{assetId}")]
    public async Task<IActionResult> SetAssetTags(int assetId, [FromBody] AssetTagsDto dto)
    {
        await _tagService.SetAssetTagsAsync(assetId, dto.TagIds, GetUserId());
        return Ok();
    }

    // POST: api/tags/asset/{assetId}/add  — Add tags to an asset
    [HttpPost("asset/{assetId}/add")]
    public async Task<IActionResult> AddAssetTags(int assetId, [FromBody] AssetTagsDto dto)
    {
        await _tagService.AddAssetTagsAsync(assetId, dto.TagIds, GetUserId());
        return Ok();
    }

    // POST: api/tags/asset/{assetId}/remove  — Remove tags from an asset
    [HttpPost("asset/{assetId}/remove")]
    public async Task<IActionResult> RemoveAssetTags(int assetId, [FromBody] AssetTagsDto dto)
    {
        await _tagService.RemoveAssetTagsAsync(assetId, dto.TagIds, GetUserId());
        return Ok();
    }

    // POST: api/tags/migrate  — Migrate legacy comma-separated tags to many-to-many
    [HttpPost("migrate")]
    public async Task<IActionResult> MigrateCommaSeparatedTags()
    {
        await _tagService.MigrateCommaSeparatedTagsAsync(GetUserId());
        return Ok(new { message = "Tag migration completed successfully." });
    }
}
