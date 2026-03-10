using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

/// <summary>CRUD operations for user collections.</summary>
/// <remarks>All endpoints require authentication. Collection-level authorization
/// is enforced in the service layer via ownership/permission checks.
/// <para><c>GetCollections</c> is intentionally un-paginated: results are user-scoped,
/// cached, and typically &lt;100 items (sidebar use-case). If per-user collection counts
/// grow beyond expected bounds, introduce <see cref="PaginationParams"/> here.</para>
/// </remarks>
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

    /// <summary>Get a single collection by ID (canonical resource endpoint).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Collection), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Collection>> GetCollection(
        [FromRoute, Range(1, int.MaxValue)] int id, CancellationToken ct = default)
        => Ok(await collectionService.GetByIdAsync(id, GetUserId(), ct));

    /// <summary>Get a collection with its items and subcollections.</summary>
    [HttpGet("{id:int}/items")]
    [ProducesResponseType(typeof(CollectionWithItemsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CollectionWithItemsResult>> GetCollectionWithItems(
        [FromRoute, Range(1, int.MaxValue)] int id,
        [FromQuery, Range(1, int.MaxValue)] int? folderId = null,
        CancellationToken ct = default)
        => Ok(await collectionService.GetWithItemsAsync(id, folderId, GetUserId(), ct));

    /// <summary>Create a new collection.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Collection), StatusCodes.Status201Created)]
    public async Task<ActionResult<Collection>> PostCollection(
        [FromBody] CreateCollectionDto dto, CancellationToken ct = default)
    {
        var userId = GetUserId();
        logger.LogInformation(LogEvents.CollectionCreated, "Creating collection '{Name}' for user {UserId}", dto.Name, userId);
        var created = await collectionService.CreateAsync(dto, userId, ct);
        return CreatedAtAction(nameof(GetCollection), new { id = created.Id }, created);
    }

    /// <summary>Partially update a collection (standard).</summary>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateCollection(
        [FromRoute, Range(1, int.MaxValue)] int id, [FromBody] UpdateCollectionDto dto, CancellationToken ct = default)
    {
        await collectionService.UpdateAsync(id, dto, GetUserId(), ct);
        return NoContent();
    }

    /// <summary>PUT backward-compat alias for PATCH update.</summary>
    [HttpPut("{id:int}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> UpdateCollectionPut(
        [FromRoute, Range(1, int.MaxValue)] int id, [FromBody] UpdateCollectionDto dto, CancellationToken ct = default)
        => await UpdateCollection(id, dto, ct);

    /// <summary>Delete a collection.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteCollection([FromRoute, Range(1, int.MaxValue)] int id, CancellationToken ct = default)
    {
        var userId = GetUserId();
        logger.LogInformation(LogEvents.CollectionDeleted, "Deleting collection {CollectionId} by user {UserId}", id, userId);
        await collectionService.DeleteAsync(id, userId, ct);
        return NoContent();
    }
}
