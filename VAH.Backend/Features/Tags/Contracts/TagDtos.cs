using System.ComponentModel.DataAnnotations;

namespace VAH.Backend.Models;

public class CreateTagDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Color { get; set; }
}

public class UpdateTagDto
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(20)]
    public string? Color { get; set; }
}

public class AssetTagsDto
{
    [Required]
    public List<int> TagIds { get; set; } = new();
}
