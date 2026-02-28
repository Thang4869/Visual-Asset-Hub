using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

/// <summary>
/// Tag CRUD and asset-tag association endpoints.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class TagsController(ITagService tagService) : BaseApiController
{
    /// <summary>Get all tags for the current user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Tag>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Tag>>> GetTags(CancellationToken ct)
        => Ok(await tagService.GetAllAsync(GetUserId(), ct));

    /// <summary>Get a single tag by ID.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Tag), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Tag>> GetTag(int id, CancellationToken ct)
        => Ok(await tagService.GetByIdAsync(id, GetUserId(), ct));

    /// <summary>Create a new tag (returns existing if duplicate name).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Tag), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Tag>> CreateTag([FromBody] CreateTagDto dto, CancellationToken ct)
    {
        var tag = await tagService.CreateAsync(dto, GetUserId(), ct);
        return CreatedAtAction(nameof(GetTag), new { id = tag.Id }, tag);
    }

    /// <summary>Update a tag's name or color.</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Tag), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Tag>> UpdateTag(int id, [FromBody] UpdateTagDto dto, CancellationToken ct)
        => Ok(await tagService.UpdateAsync(id, dto, GetUserId(), ct));

    /// <summary>Delete a tag.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTag(int id, CancellationToken ct)
    {
        await tagService.DeleteAsync(id, GetUserId(), ct);
        return NoContent();
    }

    /// <summary>Get all tags assigned to an asset.</summary>
    [HttpGet("asset/{assetId}")]
    [ProducesResponseType(typeof(List<Tag>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<Tag>>> GetAssetTags(int assetId, CancellationToken ct)
        => Ok(await tagService.GetAssetTagsAsync(assetId, GetUserId(), ct));

    /// <summary>Replace all tags on an asset.</summary>
    [HttpPut("asset/{assetId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetAssetTags(int assetId, [FromBody] AssetTagsDto dto, CancellationToken ct)
    {
        await tagService.SetAssetTagsAsync(assetId, dto.TagIds, GetUserId(), ct);
        return Ok();
    }

    /// <summary>Add tags to an asset (additive).</summary>
    [HttpPost("asset/{assetId}/add")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddAssetTags(int assetId, [FromBody] AssetTagsDto dto, CancellationToken ct)
    {
        await tagService.AddAssetTagsAsync(assetId, dto.TagIds, GetUserId(), ct);
        return Ok();
    }

    /// <summary>Remove tags from an asset.</summary>
    [HttpPost("asset/{assetId}/remove")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveAssetTags(int assetId, [FromBody] AssetTagsDto dto, CancellationToken ct)
    {
        await tagService.RemoveAssetTagsAsync(assetId, dto.TagIds, GetUserId(), ct);
        return Ok();
    }

    /// <summary>Migrate legacy comma-separated tags to many-to-many system.</summary>
    [HttpPost("migrate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MigrateCommaSeparatedTags(CancellationToken ct)
    {
        await tagService.MigrateCommaSeparatedTagsAsync(GetUserId(), ct);
        return Ok(new { message = "Tag migration completed successfully." });
    }
}
