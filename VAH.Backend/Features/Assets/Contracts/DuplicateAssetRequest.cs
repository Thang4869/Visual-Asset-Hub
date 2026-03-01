using System.ComponentModel.DataAnnotations;

namespace VAH.Backend.Features.Assets.Contracts;

public sealed class DuplicateAssetRequest
{
    [Range(1, int.MaxValue)]
    public int? TargetFolderId { get; init; }
}
