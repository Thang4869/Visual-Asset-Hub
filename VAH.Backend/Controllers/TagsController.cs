using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

/// <summary>Tag CRUD and asset-tag association endpoints.</summary>
/// <remarks>All tag operations are user-scoped. Asset-tag mutations
/// check ownership via the service layer.</remarks>
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public sealed class TagsController(
    ITagService tagService,
    ILogger<TagsController> logger) : BaseApiController
{
    /// <summary>Get all tags for the current user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Tag>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Tag>>> GetTags(CancellationToken ct = default)
        => Ok(await tagService.GetAllAsync(GetUserId(), ct));

    /// <summary>Get a single tag by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Tag), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Tag>> GetTag([FromRoute] int id, CancellationToken ct = default)
        => Ok(await tagService.GetByIdAsync(id, GetUserId(), ct));

    /// <summary>Create a new tag (returns existing if duplicate name).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Tag), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Tag>> CreateTag([FromBody] CreateTagDto dto, CancellationToken ct = default)
    {
        var userId = GetUserId();
        logger.LogInformation("Creating tag '{Name}' for user {UserId}", dto.Name, userId);
        var tag = await tagService.CreateAsync(dto, userId, ct);
        return CreatedAtAction(nameof(GetTag), new { id = tag.Id }, tag);
    }

    /// <summary>Update a tag's name or color.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(Tag), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Tag>> UpdateTag(
        [FromRoute] int id, [FromBody] UpdateTagDto dto, CancellationToken ct = default)
        => Ok(await tagService.UpdateAsync(id, dto, GetUserId(), ct));

    /// <summary>Delete a tag.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTag([FromRoute] int id, CancellationToken ct = default)
    {
        var userId = GetUserId();
        logger.LogInformation("Deleting tag {TagId} by user {UserId}", id, userId);
        await tagService.DeleteAsync(id, userId, ct);
        return NoContent();
    }

    /// <summary>Get all tags assigned to an asset.</summary>
    [HttpGet("asset/{assetId:int}")]
    [ProducesResponseType(typeof(List<Tag>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<Tag>>> GetAssetTags(
        [FromRoute] int assetId, CancellationToken ct = default)
        => Ok(await tagService.GetAssetTagsAsync(assetId, GetUserId(), ct));

    /// <summary>Replace all tags on an asset.</summary>
    [HttpPut("asset/{assetId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetAssetTags(
        [FromRoute] int assetId, [FromBody] AssetTagsDto dto, CancellationToken ct = default)
    {
        await tagService.SetAssetTagsAsync(assetId, dto.TagIds, GetUserId(), ct);
        return NoContent();
    }

    /// <summary>Add tags to an asset (additive).</summary>
    [HttpPost("asset/{assetId:int}/add")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddAssetTags(
        [FromRoute] int assetId, [FromBody] AssetTagsDto dto, CancellationToken ct = default)
    {
        await tagService.AddAssetTagsAsync(assetId, dto.TagIds, GetUserId(), ct);
        return NoContent();
    }

    /// <summary>Remove tags from an asset.</summary>
    [HttpPost("asset/{assetId:int}/remove")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveAssetTags(
        [FromRoute] int assetId, [FromBody] AssetTagsDto dto, CancellationToken ct = default)
    {
        await tagService.RemoveAssetTagsAsync(assetId, dto.TagIds, GetUserId(), ct);
        return NoContent();
    }

    /// <summary>Migrate legacy comma-separated tags to many-to-many system.</summary>
    [HttpPost("migrate")]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<MessageResult>> MigrateCommaSeparatedTags(CancellationToken ct = default)
    {
        logger.LogWarning("Tag migration triggered by user {UserId}", GetUserId());
        await tagService.MigrateCommaSeparatedTagsAsync(GetUserId(), ct);
        return Ok(new MessageResult("Tag migration completed successfully."));
    }
}
