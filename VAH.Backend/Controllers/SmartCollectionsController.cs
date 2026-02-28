using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

/// <summary>
/// Smart (auto-categorized) collections — virtual collections computed from rules.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class SmartCollectionsController(ISmartCollectionService smartService) : BaseApiController
{
    /// <summary>Get all available smart collection definitions for the current user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<SmartCollectionDefinition>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SmartCollectionDefinition>>> GetSmartCollections(CancellationToken ct)
        => Ok(await smartService.GetDefinitionsAsync(GetUserId(), ct));

    /// <summary>Get paginated items matching a smart collection’s criteria.</summary>
    [HttpGet("{id}/items")]
    [ProducesResponseType(typeof(PagedResult<AssetResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AssetResponseDto>>> GetSmartCollectionItems(
        string id, [FromQuery] PaginationParams pagination, CancellationToken ct)
        => Ok(await smartService.GetItemsAsync(id, pagination, GetUserId(), ct));
}
