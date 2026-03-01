using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace VAH.Backend.Features.Assets.Contracts;

public sealed class UploadAssetsRequest
{
    [Required, MinLength(1)]
    public List<IFormFile> Files { get; init; } = new();

    [Range(1, int.MaxValue)]
    public int? CollectionId { get; init; }

    [Range(1, int.MaxValue)]
    public int? FolderId { get; init; }
}
