using System.ComponentModel.DataAnnotations;

namespace VAH.Backend.Models;

/// <summary>
/// Represents a collection/category for organizing assets.
/// Supports hierarchical nesting via ParentId.
/// </summary>
public class Collection
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    public int? ParentId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(20)]
    public string Color { get; set; } = "#007bff";

    [MaxLength(50)]
    public string Type { get; set; } = "default";

    public int Order { get; set; } = 0;

    [MaxLength(20)]
    public string LayoutType { get; set; } = "grid";
}
