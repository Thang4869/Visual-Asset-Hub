using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace VAH.Backend.Features.Search.Queries;

public record GlobalSearchResultDto(string Id, string Type, string Name, string? ThumbnailUrl);

/// <summary>
/// Query tìm kiếm toàn cục cho Navigation Bar.
/// </summary>
public record GlobalSearchQuery(string UserId, string Query, string? Type, int? CollectionId, int Page, int PageSize) : IRequest<List<GlobalSearchResultDto>>;

public class GlobalSearchQueryHandler : IRequestHandler<GlobalSearchQuery, List<GlobalSearchResultDto>>
{
    public Task<List<GlobalSearchResultDto>> Handle(GlobalSearchQuery request, CancellationToken ct)
    {
        // 1. Mock validation cho Query Parameter
        if (string.IsNullOrWhiteSpace(request.Query))
            throw new System.ArgumentException("Query parameter is required.");

        // 2. Tương tác với Database thực tế sau này (EntityFramework, ElasticSearch, v.v...)
        // await _dbContext.Assets.Where(a => a.Name.Contains(request.Query)).Select(...).ToListAsync(ct);

        var mockupData = new List<GlobalSearchResultDto>
        {
            new GlobalSearchResultDto(System.Guid.NewGuid().ToString(), "Asset", $"Kết quả mock cho '{request.Query}'", "https://mock.image")
        };

        return Task.FromResult(mockupData);
    }
}
