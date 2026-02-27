using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VAH.Backend.Models;

/// <summary>
/// Represents a tag that can be applied to multiple assets.
/// Many-to-many relationship via AssetTag junction table.
/// </summary>
public class Tag
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Normalized lowercase name for dedup + search.</summary>
    [Required, MaxLength(100)]
    public string NormalizedName { get; set; } = string.Empty;

    /// <summary>Optional color for tag badge display.</summary>
    [MaxLength(20)]
    public string? Color { get; set; }

    /// <summary>Owner of this tag. Null for system/shared tags.</summary>
    public string? UserId { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation
    [JsonIgnore]
    public ICollection<AssetTag> AssetTags { get; set; } = new List<AssetTag>();

    // ── Domain behavior methods ──

    /// <summary>Set name and auto-compute normalized form.</summary>
    public void SetName(string name)
    {
        Name = name.Trim();
        NormalizedName = name.Trim().ToLowerInvariant();
    }

    /// <summary>Apply partial update from DTO.</summary>
    public void UpdateFrom(UpdateTagDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.Name))
            SetName(dto.Name);
        if (dto.Color != null)
            Color = dto.Color;
    }

    /// <summary>Check if this tag is owned by a specific user.</summary>
    public bool IsOwnedBy(string userId) => UserId == userId;
}

/// <summary>
/// Junction table for Asset ↔ Tag many-to-many relationship.
/// </summary>
public class AssetTag
{
    public int AssetId { get; set; }
    public int TagId { get; set; }

    [JsonIgnore]
    public Asset? Asset { get; set; }
    public Tag? Tag { get; set; }
}
