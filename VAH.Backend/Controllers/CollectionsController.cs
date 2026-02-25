using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CollectionsController : ControllerBase
{
    private readonly ICollectionService _collectionService;

    public CollectionsController(ICollectionService collectionService)
    {
        _collectionService = collectionService;
    }

    // GET: api/collections
    [HttpGet]
    public async Task<ActionResult<List<Collection>>> GetCollections()
    {
        var collections = await _collectionService.GetAllAsync();
        return Ok(collections);
    }

    // GET: api/collections/{id}/items?folderId=
    [HttpGet("{id}/items")]
    public async Task<ActionResult<CollectionWithItemsResult>> GetCollectionWithItems(
        int id, [FromQuery] int? folderId = null)
    {
        var result = await _collectionService.GetWithItemsAsync(id, folderId);
        return Ok(result);
    }

    // POST: api/collections
    [HttpPost]
    public async Task<ActionResult<Collection>> PostCollection(Collection collection)
    {
        var created = await _collectionService.CreateAsync(collection);
        return CreatedAtAction(nameof(GetCollections), new { id = created.Id }, created);
    }

    // PUT: api/collections/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCollection(int id, Collection collection)
    {
        await _collectionService.UpdateAsync(id, collection);
        return NoContent();
    }

    // DELETE: api/collections/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCollection(int id)
    {
        await _collectionService.DeleteAsync(id);
        return NoContent();
    }
}
