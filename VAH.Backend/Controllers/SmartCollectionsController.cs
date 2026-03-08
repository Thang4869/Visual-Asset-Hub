using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Controllers;

/// <summary>Smart (auto-categorized) collections — virtual collections computed from rules.</summary>
/// <remarks>These collections are dynamically generated and not persisted.
/// <para>The <c>id</c> route parameter must match a known <see cref="SmartCollectionDefinition.Id"/>.
/// Invalid identifiers return 400 ProblemDetails.</para></remarks>
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public sealed class SmartCollectionsController(ISmartCollectionService smartService) : BaseApiController
{
    /// <summary>Get all available smart collection definitions for the current user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<SmartCollectionDefinition>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SmartCollectionDefinition>>> GetSmartCollections(CancellationToken ct = default)
        => Ok(await smartService.GetDefinitionsAsync(GetUserId(), ct));

    /// <summary>Get paginated items matching a smart collection’s criteria.</summary>
    [HttpGet("{id}/items")]
    [ProducesResponseType(typeof(PagedResult<AssetResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AssetResponseDto>>> GetSmartCollectionItems(
        [FromRoute, RegularExpression(@"^[a-z0-9\-]+$")] string id,
        [FromQuery] PaginationParams pagination,
        CancellationToken ct = default)
        => Ok(await smartService.GetItemsAsync(id, pagination, GetUserId(), ct));
}
