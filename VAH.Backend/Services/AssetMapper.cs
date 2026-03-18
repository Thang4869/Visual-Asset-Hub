using System.Collections.Generic;
using System.Linq;
using VAH.Backend.Models;

namespace VAH.Backend.Services;

/// <summary>
/// Maps between Asset domain entities and DTOs.
/// Injectable implementation to keep mapping testable.
/// </summary>
public class AssetMapper : IAssetMapper
{
    public AssetResponseDto ToDto(Asset asset) => new()
    {
        Id = asset.Id,
        FileName = asset.FileName,
        FilePath = asset.FilePath,
#pragma warning disable CS0618 // Obsolete Tags — kept for API backward compat
        Tags = asset.Tags,
#pragma warning restore CS0618
        CreatedAt = asset.CreatedAt,
        PositionX = asset.PositionX,
        PositionY = asset.PositionY,
        CollectionId = asset.CollectionId,
        ContentType = asset.ContentType,
        GroupId = asset.GroupId,
        ParentFolderId = asset.ParentFolderId,
        SortOrder = asset.SortOrder,
        IsFolder = asset.IsFolder,
        ThumbnailSm = asset.ThumbnailSm,
        ThumbnailMd = asset.ThumbnailMd,
        ThumbnailLg = asset.ThumbnailLg,
    };

    public List<AssetResponseDto> ToDtoList(IEnumerable<Asset> assets)
        => assets.Select(ToDto).ToList();

    public Asset CreateFileFromDto(CreateAssetDto dto, string userId)
        => AssetFactory.CreateFile(dto.FileName.Trim(), dto.FilePath.Trim(), dto.CollectionId, userId, dto.ParentFolderId);
}
