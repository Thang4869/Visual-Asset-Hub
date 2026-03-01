using System.Collections.Generic;
using MediatR;
using VAH.Backend.Models;

namespace VAH.Backend.CQRS.Assets.Commands;

/// <summary>Command: Create a generic asset from metadata.</summary>
public sealed record CreateAssetCommand(
    CreateAssetDto Dto,
    string UserId) : IRequest<AssetResponseDto>;

/// <summary>Command: Upload one or more files to a collection.</summary>
public sealed record UploadFilesCommand(
    IReadOnlyCollection<UploadedFileDto> Files,
    int CollectionId,
    int? FolderId,
    string UserId) : IRequest<List<AssetResponseDto>>;

/// <summary>Command: Partial update of an asset (rename, move, regroup).</summary>
public sealed record UpdateAssetCommand(
    int Id,
    UpdateAssetDto Dto,
    string UserId) : IRequest<AssetResponseDto>;

/// <summary>Command: Delete an asset and its associated files/thumbnails.</summary>
public sealed record DeleteAssetCommand(
    int Id,
    string UserId) : IRequest<bool>;

/// <summary>Command: Duplicate an asset in-place (same folder).</summary>
public sealed record DuplicateAssetCommand(
    int Id,
    int? TargetFolderId,
    string UserId) : IRequest<AssetResponseDto>;
