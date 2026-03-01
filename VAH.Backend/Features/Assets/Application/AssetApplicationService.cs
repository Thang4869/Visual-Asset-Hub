using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Options;
using VAH.Backend.Configuration;
using VAH.Backend.CQRS.Assets.Commands;
using VAH.Backend.CQRS.Assets.Queries;
using VAH.Backend.Features.Assets.Application.Duplicate;
using VAH.Backend.Features.Assets.Infrastructure.Contexts;
using VAH.Backend.Models;

namespace VAH.Backend.Features.Assets.Application;

internal sealed class AssetApplicationService : IAssetApplicationService
{
    private readonly ISender _sender;
    private readonly IUserContextProvider _userContextProvider;
    private readonly IAssetDuplicateStrategyFactory _duplicateStrategyFactory;
    private readonly AssetOptions _assetOptions;

    public AssetApplicationService(
        ISender sender,
        IUserContextProvider userContextProvider,
        IAssetDuplicateStrategyFactory duplicateStrategyFactory,
        IOptions<AssetOptions> assetOptions)
    {
        _sender = sender;
        _userContextProvider = userContextProvider;
        _duplicateStrategyFactory = duplicateStrategyFactory;
        _assetOptions = assetOptions.Value ?? new AssetOptions();
    }

    private string CurrentUserId => _userContextProvider.GetUserId();
    private int DefaultCollectionId => _assetOptions.DefaultCollectionId;

    public Task<PagedResult<AssetResponseDto>> GetAssetsAsync(PaginationParams pagination, CancellationToken ct = default)
        => _sender.Send(new GetAssetsQuery(pagination, CurrentUserId), ct);

    public Task<AssetResponseDto> GetAssetByIdAsync(int id, CancellationToken ct = default)
        => _sender.Send(new GetAssetByIdQuery(id, CurrentUserId), ct);

    public Task<List<AssetResponseDto>> GetAssetsByGroupAsync(int groupId, CancellationToken ct = default)
        => _sender.Send(new GetAssetsByGroupQuery(groupId, CurrentUserId), ct);

    public Task<AssetResponseDto> CreateAssetAsync(CreateAssetDto dto, CancellationToken ct = default)
        => _sender.Send(new CreateAssetCommand(dto, CurrentUserId), ct);

    public Task<List<AssetResponseDto>> UploadFilesAsync(IReadOnlyCollection<UploadedFileDto> files, int? collectionId, int? folderId, CancellationToken ct = default)
    {
        var targetCollectionId = collectionId ?? DefaultCollectionId;
        var payload = files ?? Array.Empty<UploadedFileDto>();
        return _sender.Send(new UploadFilesCommand(payload, targetCollectionId, folderId, CurrentUserId), ct);
    }

    public Task<AssetResponseDto> UpdateAssetAsync(int id, UpdateAssetDto dto, CancellationToken ct = default)
        => _sender.Send(new UpdateAssetCommand(id, dto, CurrentUserId), ct);

    public Task DeleteAssetAsync(int id, CancellationToken ct = default)
        => _sender.Send(new DeleteAssetCommand(id, CurrentUserId), ct);

    public Task<AssetResponseDto> DuplicateAssetAsync(int id, int? targetFolderId, CancellationToken ct = default)
    {
        var strategy = _duplicateStrategyFactory.Create(targetFolderId);
        return strategy.DuplicateAsync(id, targetFolderId, ct);
    }
}
