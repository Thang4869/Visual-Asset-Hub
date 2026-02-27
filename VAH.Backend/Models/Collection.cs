using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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

    public DateTime CreatedAt { get; set; }

    [MaxLength(20)]
    public string Color { get; set; } = "#007bff";

    public CollectionType Type { get; set; } = CollectionType.Default;

    public int Order { get; set; } = 0;

    public LayoutType LayoutType { get; set; } = LayoutType.Grid;

    /// <summary>
    /// Owner of this collection. Null for system/shared collections.
    /// </summary>
    public string? UserId { get; set; }

    // --- Navigation properties ---

    /// <summary>Assets in this collection.</summary>
    [JsonIgnore]
    public ICollection<Asset> Assets { get; set; } = new List<Asset>();

    /// <summary>Parent collection (self-referencing via ParentId).</summary>
    [JsonIgnore]
    public Collection? Parent { get; set; }

    /// <summary>Child collections.</summary>
    [JsonIgnore]
    public ICollection<Collection> Children { get; set; } = new List<Collection>();

    // ── Domain behavior methods ──

    /// <summary>Check if this collection is owned by a specific user.</summary>
    public bool IsOwnedBy(string userId) => UserId == userId;

    /// <summary>Whether this is a system/shared collection (no owner).</summary>
    public bool IsSystemCollection => UserId == null;

    /// <summary>Check if user has access (owner or system collection).</summary>
    public bool IsAccessibleBy(string userId) => IsSystemCollection || IsOwnedBy(userId);

    /// <summary>Apply partial update from another collection object.</summary>
    public void ApplyUpdate(Collection source)
    {
        Name = source.Name?.Trim() ?? Name;
        Description = source.Description ?? Description;
        Color = source.Color ?? Color;
        Type = source.Type;
        Order = source.Order;
        LayoutType = source.LayoutType;
    }
}
