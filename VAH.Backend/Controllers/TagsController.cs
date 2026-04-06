using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VAH.Backend.Models;
using VAH.Backend.Features.Tags.Commands;
using VAH.Backend.Features.Tags.Queries;

namespace VAH.Backend.Controllers;

/// <summary>Tag CRUD and asset-tag association endpoints.</summary>
/// <remarks>All tag operations are user-scoped. Asset-tag mutations
/// check ownership via the CQRS Handlers.</remarks>
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public sealed class TagsController(
    ISender sender,
    ILogger<TagsController> logger) : BaseApiController
{
    private readonly ISender _sender = sender;

    /// <summary>Get all tags for the current user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Tag>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Tag>>> GetTags(CancellationToken ct = default)
        => Ok(await _sender.Send(new GetAllTagsQuery(GetUserId()), ct));

    /// <summary>Get a single tag by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Tag), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Tag>> GetTag([FromRoute] int id, CancellationToken ct = default)
        => Ok(await _sender.Send(new GetTagByIdQuery(id, GetUserId()), ct));

    /// <summary>Create a new tag (returns existing if duplicate name).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Tag), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Tag), StatusCodes.Status200OK)]
    public async Task<ActionResult<Tag>> CreateTag([FromBody] CreateTagDto dto, CancellationToken ct = default)
    {
        var userId = GetUserId();
        logger.LogInformation(LogEvents.TagCreated, "Creating tag '{Name}' by user {UserId}", dto.Name, userId);
        
        var (tag, created) = await _sender.Send(new CreateTagCommand(dto, userId), ct);
        return created
            ? CreatedAtAction(nameof(GetTag), new { id = tag.Id }, tag)
            : Ok(tag);
    }

    /// <summary>Update a tag's name or color.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(Tag), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Tag>> UpdateTag(
        [FromRoute] int id, [FromBody] UpdateTagDto dto, CancellationToken ct = default)
        => Ok(await _sender.Send(new UpdateTagCommand(id, dto, GetUserId()), ct));

    /// <summary>Delete a tag.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTag([FromRoute] int id, CancellationToken ct = default)
    {
        var userId = GetUserId();
        logger.LogInformation(LogEvents.TagDeleted, "Deleting tag {TagId} by user {UserId}", id, userId);
        await _sender.Send(new DeleteTagCommand(id, userId), ct);
        return NoContent();
    }

    /// <summary>Get all tags assigned to an asset.</summary>
    [HttpGet("asset/{assetId:int}")]
    [ProducesResponseType(typeof(List<Tag>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<Tag>>> GetAssetTags(
        [FromRoute] int assetId, CancellationToken ct = default)
        => Ok(await _sender.Send(new GetAssetTagsQuery(assetId, GetUserId()), ct));

    /// <summary>Replace all tags on an asset.</summary>
    [HttpPut("asset/{assetId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetAssetTags(
        [FromRoute] int assetId, [FromBody] AssetTagsDto dto, CancellationToken ct = default)
    {
        await _sender.Send(new SetAssetTagsCommand(assetId, dto, GetUserId()), ct);
        return NoContent();
    }

    /// <summary>Add tags to an asset.</summary>
    [HttpPost("asset/{assetId:int}/add")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddAssetTags(
        [FromRoute] int assetId, [FromBody] AssetTagsDto dto, CancellationToken ct = default)
    {
        await _sender.Send(new AddAssetTagsCommand(assetId, dto, GetUserId()), ct);
        return NoContent();
    }

    /// <summary>Remove tags from an asset.</summary>
    [HttpPost("asset/{assetId:int}/remove")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveAssetTags(
        [FromRoute] int assetId, [FromBody] AssetTagsDto dto, CancellationToken ct = default)
    {
        await _sender.Send(new RemoveAssetTagsCommand(assetId, dto, GetUserId()), ct);
        return NoContent();
    }

    /// <summary>Migrate legacy tags.</summary>
    [HttpPost("migrate")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting(RateLimitPolicies.Fixed)]
    [ProducesResponseType(typeof(MessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MessageResult>> MigrateCommaSeparatedTags(CancellationToken ct = default)
    {
        logger.LogWarning(LogEvents.TagMigration, "Tag migration triggered by user {UserId}", GetUserId());
        await _sender.Send(new MigrateTagsCommand(GetUserId()), ct);
        return Ok(new MessageResult("Tag migration completed successfully."));
    }
}
