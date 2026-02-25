using System.ComponentModel.DataAnnotations;

namespace VAH.Backend.Models;

/// <summary>
/// Represents a digital asset in the system.
/// Can be: image, link, color, folder, or color-group.
/// </summary>
public class Asset
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(500)]
    public string FileName { get; set; } = string.Empty;

    [Required, MaxLength(2048)]
    public string FilePath { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Tags { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public double PositionX { get; set; } = 0;
    public double PositionY { get; set; } = 0;

    public int CollectionId { get; set; } = 1;

    [MaxLength(50)]
    public string ContentType { get; set; } = "image";

    public int? GroupId { get; set; } = null;
    public int? ParentFolderId { get; set; } = null;
    public int SortOrder { get; set; } = 0;
    public bool IsFolder { get; set; } = false;
}