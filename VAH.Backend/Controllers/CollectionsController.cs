using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

/// <summary>
/// CRUD operations for user collections.
/// </summary>
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class CollectionsController(ICollectionService collectionService) : BaseApiController
{
    /// <summary>Get all collections accessible to the current user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Collection>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Collection>>> GetCollections(CancellationToken ct)
        => Ok(await collectionService.GetAllAsync(GetUserId(), ct));

    /// <summary>Get a collection with its items and subcollections.</summary>
    [HttpGet("{id}/items")]
    [ProducesResponseType(typeof(CollectionWithItemsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CollectionWithItemsResult>> GetCollectionWithItems(
        int id, [FromQuery] int? folderId = null, CancellationToken ct = default)
        => Ok(await collectionService.GetWithItemsAsync(id, folderId, GetUserId(), ct));

    /// <summary>Create a new collection.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Collection), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Collection>> PostCollection([FromBody] CreateCollectionDto dto, CancellationToken ct)
    {
        var created = await collectionService.CreateAsync(dto, GetUserId(), ct);
        return CreatedAtAction(nameof(GetCollectionWithItems), new { id = created.Id }, created);
    }

    /// <summary>Partially update a collection (standard).</summary>
    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCollection(int id, [FromBody] UpdateCollectionDto dto, CancellationToken ct)
    {
        await collectionService.UpdateAsync(id, dto, GetUserId(), ct);
        return NoContent();
    }

    /// <summary>PUT backward-compat alias for PATCH update.</summary>
    [HttpPut("{id}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public Task<IActionResult> UpdateCollectionPut(int id, [FromBody] UpdateCollectionDto dto, CancellationToken ct)
        => UpdateCollection(id, dto, ct);

    /// <summary>Delete a collection.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCollection(int id, CancellationToken ct)
    {
        await collectionService.DeleteAsync(id, GetUserId(), ct);
        return NoContent();
    }
}
