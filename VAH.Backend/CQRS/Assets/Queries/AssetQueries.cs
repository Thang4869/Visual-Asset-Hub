using MediatR;
using VAH.Backend.Models;

namespace VAH.Backend.CQRS.Assets.Queries;

/// <summary>Query: Paginated list of the current user's assets.</summary>
public sealed record GetAssetsQuery(
    PaginationParams Pagination,
    string UserId) : IRequest<PagedResult<AssetResponseDto>>;

/// <summary>Query: Single asset by ID.</summary>
public sealed record GetAssetByIdQuery(
    int Id,
    string UserId) : IRequest<AssetResponseDto>;

/// <summary>Query: Assets belonging to a color group.</summary>
public sealed record GetAssetsByGroupQuery(
    int GroupId,
    string UserId) : IRequest<IReadOnlyList<AssetResponseDto>>;
