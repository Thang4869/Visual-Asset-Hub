using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VAH.Backend.CQRS.Assets.Commands;
using VAH.Backend.Features.Assets.Infrastructure.Contexts;
using VAH.Backend.Models;

namespace VAH.Backend.Features.Assets.Application.Duplicate;

internal sealed class TargetFolderDuplicateStrategy : IAssetDuplicateStrategy
{
    private readonly ISender _sender;
    private readonly IUserContextProvider _userContextProvider;

    public TargetFolderDuplicateStrategy(ISender sender, IUserContextProvider userContextProvider)
    {
        _sender = sender;
        _userContextProvider = userContextProvider;
    }

    public bool CanHandle(int? targetFolderId) => targetFolderId.HasValue;

    public Task<AssetResponseDto> DuplicateAsync(int assetId, int? targetFolderId, CancellationToken cancellationToken = default)
        => _sender.Send(new DuplicateAssetCommand(assetId, targetFolderId, _userContextProvider.GetUserId()), cancellationToken);
}
