using VAH.Backend.Models;

namespace VAH.Backend.Services;

/// <summary>
/// Maps between Asset domain entities and DTOs.
/// Keeps the Domain layer free from DTO dependencies.
/// </summary>
public static class AssetMapper
{
    /// <summary>Map a single Asset entity to the API response DTO.</summary>
    public static AssetResponseDto ToDto(Asset asset) => new()
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

    /// <summary>Map a list of Asset entities to response DTOs.</summary>
    public static List<AssetResponseDto> ToDtoList(IEnumerable<Asset> assets)
        => assets.Select(ToDto).ToList();

    /// <summary>
    /// Create a generic file Asset from primitive parameters.
    /// Replaces the removed AssetFactory.FromDto to keep Factory DTO-free.
    /// </summary>
    public static Asset CreateFileFromDto(CreateAssetDto dto, string userId)
        => AssetFactory.CreateFile(dto.FileName.Trim(), dto.FilePath.Trim(), dto.CollectionId, userId, dto.ParentFolderId);
}
