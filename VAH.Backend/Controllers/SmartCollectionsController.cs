using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

/// <summary>
/// Smart (auto-categorized) collections — virtual collections computed from rules.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class SmartCollectionsController : ControllerBase
{
    private readonly ISmartCollectionService _smartService;

    public SmartCollectionsController(ISmartCollectionService smartService)
    {
        _smartService = smartService;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User identity not found.");

    // GET: api/smart-collections
    [HttpGet]
    public async Task<ActionResult<List<SmartCollectionDefinition>>> GetSmartCollections()
    {
        var definitions = await _smartService.GetDefinitionsAsync(GetUserId());
        return Ok(definitions);
    }

    // GET: api/smart-collections/{id}/items?page=1&pageSize=50
    [HttpGet("{id}/items")]
    public async Task<ActionResult<PagedResult<Asset>>> GetSmartCollectionItems(
        string id, [FromQuery] PaginationParams pagination)
    {
        var result = await _smartService.GetItemsAsync(id, pagination, GetUserId());
        return Ok(result);
    }
}
