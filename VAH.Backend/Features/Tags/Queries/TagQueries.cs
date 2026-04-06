using MediatR;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Features.Tags.Queries;

public record GetAllTagsQuery(string UserId) : IRequest<List<Tag>>;
public class GetAllTagsQueryHandler(ITagService tagService) : IRequestHandler<GetAllTagsQuery, List<Tag>>
{
    public Task<List<Tag>> Handle(GetAllTagsQuery request, CancellationToken cancellationToken)
        => tagService.GetAllAsync(request.UserId, cancellationToken);
}

public record GetTagByIdQuery(int Id, string UserId) : IRequest<Tag>;
public class GetTagByIdQueryHandler(ITagService tagService) : IRequestHandler<GetTagByIdQuery, Tag>
{
    public Task<Tag> Handle(GetTagByIdQuery request, CancellationToken cancellationToken)
        => tagService.GetByIdAsync(request.Id, request.UserId, cancellationToken);
}

public record GetAssetTagsQuery(int AssetId, string UserId) : IRequest<List<Tag>>;
public class GetAssetTagsQueryHandler(ITagService tagService) : IRequestHandler<GetAssetTagsQuery, List<Tag>>
{
    public Task<List<Tag>> Handle(GetAssetTagsQuery request, CancellationToken cancellationToken)
        => tagService.GetAssetTagsAsync(request.AssetId, request.UserId, cancellationToken);
}
