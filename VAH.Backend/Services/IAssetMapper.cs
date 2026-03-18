using System.Collections.Generic;
using VAH.Backend.Models;

namespace VAH.Backend.Services;

public interface IAssetMapper
{
    AssetResponseDto ToDto(Asset asset);
    List<AssetResponseDto> ToDtoList(IEnumerable<Asset> assets);
    Asset CreateFileFromDto(CreateAssetDto dto, string userId);
}
