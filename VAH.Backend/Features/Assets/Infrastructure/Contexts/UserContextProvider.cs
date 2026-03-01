using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace VAH.Backend.Features.Assets.Infrastructure.Contexts;

/// <summary>
/// Encapsulates how we resolve the current user identifier for downstream services.
/// </summary>
internal sealed class UserContextProvider : IUserContextProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContextProvider(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    public string GetUserId() =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User identity not found.");
}
