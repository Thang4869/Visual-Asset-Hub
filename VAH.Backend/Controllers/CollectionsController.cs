using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

/// <summary>CRUD operations for user collections.</summary>
/// <remarks>All endpoints require authentication. Collection-level authorization
/// is enforced in the service layer via ownership/permission checks.</remarks>
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public sealed class CollectionsController(
    ICollectionService collectionService,
    ILogger<CollectionsController> logger) : BaseApiController
{
    /// <summary>Get all collections accessible to the current user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Collection>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Collection>>> GetCollections(CancellationToken ct = default)
        => Ok(await collectionService.GetAllAsync(GetUserId(), ct));

    /// <summary>Get a collection with its items and subcollections.</summary>
    [HttpGet("{id:int}/items")]
    [ProducesResponseType(typeof(CollectionWithItemsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CollectionWithItemsResult>> GetCollectionWithItems(
        [FromRoute] int id, [FromQuery] int? folderId = null, CancellationToken ct = default)
        => Ok(await collectionService.GetWithItemsAsync(id, folderId, GetUserId(), ct));

    /// <summary>Create a new collection.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Collection), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Collection>> PostCollection(
        [FromBody] CreateCollectionDto dto, CancellationToken ct = default)
    {
        var userId = GetUserId();
        logger.LogInformation("Creating collection '{Name}' for user {UserId}", dto.Name, userId);
        var created = await collectionService.CreateAsync(dto, userId, ct);
        return CreatedAtAction(nameof(GetCollectionWithItems), new { id = created.Id }, created);
    }

    /// <summary>Partially update a collection (standard).</summary>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCollection(
        [FromRoute] int id, [FromBody] UpdateCollectionDto dto, CancellationToken ct = default)
    {
        await collectionService.UpdateAsync(id, dto, GetUserId(), ct);
        return NoContent();
    }

    /// <summary>PUT backward-compat alias for PATCH update.</summary>
    [HttpPut("{id:int}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public Task<IActionResult> UpdateCollectionPut(
        [FromRoute] int id, [FromBody] UpdateCollectionDto dto, CancellationToken ct = default)
        => UpdateCollection(id, dto, ct);

    /// <summary>Delete a collection.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCollection([FromRoute] int id, CancellationToken ct = default)
    {
        var userId = GetUserId();
        logger.LogInformation("Deleting collection {CollectionId} by user {UserId}", id, userId);
        await collectionService.DeleteAsync(id, userId, ct);
        return NoContent();
    }
}
