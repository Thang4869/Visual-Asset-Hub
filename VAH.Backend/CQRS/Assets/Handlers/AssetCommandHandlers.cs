using MediatR;
using VAH.Backend.CQRS.Assets.Commands;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.CQRS.Assets.Handlers;

/// <summary>Handler: Create a generic asset from metadata.</summary>
public sealed class CreateAssetHandler(IAssetService assetService)
    : IRequestHandler<CreateAssetCommand, AssetResponseDto>
{
    public Task<AssetResponseDto> Handle(CreateAssetCommand request, CancellationToken ct)
        => assetService.CreateAssetAsync(request.Dto, request.UserId, ct);
}

/// <summary>Handler: Upload one or more files to a collection.</summary>
public sealed class UploadFilesHandler(IAssetService assetService)
    : IRequestHandler<UploadFilesCommand, List<AssetResponseDto>>
{
    public Task<List<AssetResponseDto>> Handle(UploadFilesCommand request, CancellationToken ct)
        => assetService.UploadFilesAsync(request.Files, request.CollectionId, request.FolderId, request.UserId, ct);
}

/// <summary>Handler: Partial update of an asset.</summary>
public sealed class UpdateAssetHandler(IAssetService assetService)
    : IRequestHandler<UpdateAssetCommand, AssetResponseDto>
{
    public Task<AssetResponseDto> Handle(UpdateAssetCommand request, CancellationToken ct)
        => assetService.UpdateAssetAsync(request.Id, request.Dto, request.UserId, ct);
}

/// <summary>Handler: Delete an asset and its associated files.</summary>
public sealed class DeleteAssetHandler(IAssetService assetService)
    : IRequestHandler<DeleteAssetCommand, bool>
{
    public Task<bool> Handle(DeleteAssetCommand request, CancellationToken ct)
        => assetService.DeleteAssetAsync(request.Id, request.UserId, ct);
}

/// <summary>Handler: Duplicate (clone) an existing asset.</summary>
public sealed class DuplicateAssetHandler(IAssetService assetService)
    : IRequestHandler<DuplicateAssetCommand, AssetResponseDto>
{
    public Task<AssetResponseDto> Handle(DuplicateAssetCommand request, CancellationToken ct)
        => assetService.DuplicateAssetAsync(request.Id, request.TargetFolderId, request.UserId, ct);
}
