using MediatR;
using VAH.Backend.CQRS.Assets.Queries;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.CQRS.Assets.Handlers;

/// <summary>Handler: Paginated list of the current user's assets.</summary>
public sealed class GetAssetsHandler(IAssetService assetService)
    : IRequestHandler<GetAssetsQuery, PagedResult<AssetResponseDto>>
{
    public Task<PagedResult<AssetResponseDto>> Handle(GetAssetsQuery request, CancellationToken ct)
        => assetService.GetAssetsAsync(request.Pagination, request.UserId, ct);
}

/// <summary>Handler: Single asset by ID.</summary>
public sealed class GetAssetByIdHandler(IAssetService assetService)
    : IRequestHandler<GetAssetByIdQuery, AssetResponseDto>
{
    public Task<AssetResponseDto> Handle(GetAssetByIdQuery request, CancellationToken ct)
        => assetService.GetByIdAsync(request.Id, request.UserId, ct);
}

/// <summary>Handler: Assets belonging to a color group.</summary>
public sealed class GetAssetsByGroupHandler(IAssetService assetService)
    : IRequestHandler<GetAssetsByGroupQuery, IReadOnlyList<AssetResponseDto>>
{
    public Task<IReadOnlyList<AssetResponseDto>> Handle(GetAssetsByGroupQuery request, CancellationToken ct)
        => assetService.GetAssetsByGroupAsync(request.GroupId, request.UserId, ct);
}
