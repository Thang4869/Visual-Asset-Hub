using MediatR;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Features.Tags.Commands;

public record CreateTagCommand(CreateTagDto Dto, string UserId) : IRequest<(Tag Tag, bool Created)>;
public class CreateTagCommandHandler(ITagService tagService) : IRequestHandler<CreateTagCommand, (Tag Tag, bool Created)>
{
    public Task<(Tag Tag, bool Created)> Handle(CreateTagCommand request, CancellationToken cancellationToken)
        => tagService.CreateOrGetAsync(request.Dto, request.UserId, cancellationToken);
}

public record UpdateTagCommand(int Id, UpdateTagDto Dto, string UserId) : IRequest<Tag>;
public class UpdateTagCommandHandler(ITagService tagService) : IRequestHandler<UpdateTagCommand, Tag>
{
    public Task<Tag> Handle(UpdateTagCommand request, CancellationToken cancellationToken)
        => tagService.UpdateAsync(request.Id, request.Dto, request.UserId, cancellationToken);
}

public record DeleteTagCommand(int Id, string UserId) : IRequest;
public class DeleteTagCommandHandler(ITagService tagService) : IRequestHandler<DeleteTagCommand>
{
    public async Task Handle(DeleteTagCommand request, CancellationToken cancellationToken)
    {
        await tagService.DeleteAsync(request.Id, request.UserId, cancellationToken);
    }
}

public record SetAssetTagsCommand(int AssetId, AssetTagsDto Dto, string UserId) : IRequest;
public class SetAssetTagsCommandHandler(ITagService tagService) : IRequestHandler<SetAssetTagsCommand>
{
    public async Task Handle(SetAssetTagsCommand request, CancellationToken cancellationToken)
    {
        await tagService.SetAssetTagsAsync(request.AssetId, request.Dto.TagIds, request.UserId, cancellationToken);
    }
}

public record AddAssetTagsCommand(int AssetId, AssetTagsDto Dto, string UserId) : IRequest;
public class AddAssetTagsCommandHandler(ITagService tagService) : IRequestHandler<AddAssetTagsCommand>
{
    public async Task Handle(AddAssetTagsCommand request, CancellationToken cancellationToken)
    {
        await tagService.AddAssetTagsAsync(request.AssetId, request.Dto.TagIds, request.UserId, cancellationToken);
    }
}

public record RemoveAssetTagsCommand(int AssetId, AssetTagsDto Dto, string UserId) : IRequest;
public class RemoveAssetTagsCommandHandler(ITagService tagService) : IRequestHandler<RemoveAssetTagsCommand>
{
    public async Task Handle(RemoveAssetTagsCommand request, CancellationToken cancellationToken)
    {
        await tagService.RemoveAssetTagsAsync(request.AssetId, request.Dto.TagIds, request.UserId, cancellationToken);
    }
}

public record MigrateTagsCommand(string UserId) : IRequest;
public class MigrateTagsCommandHandler(ITagService tagService) : IRequestHandler<MigrateTagsCommand>
{
    public async Task Handle(MigrateTagsCommand request, CancellationToken cancellationToken)
    {
        await tagService.MigrateCommaSeparatedTagsAsync(request.UserId, cancellationToken);
    }
}
