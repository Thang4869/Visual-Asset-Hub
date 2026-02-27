using System.ComponentModel.DataAnnotations;

namespace VAH.Backend.Models;

/// <summary>
/// Role-based permission for a collection.
/// Determines what a user can do with a specific collection.
/// </summary>
public class CollectionPermission
{
    [Key]
    public int Id { get; set; }

    /// <summary>User who has this permission.</summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>Collection this permission applies to.</summary>
    public int CollectionId { get; set; }

    /// <summary>Role: owner, editor, viewer.</summary>
    [Required, MaxLength(20)]
    public string Role { get; set; } = "viewer";

    /// <summary>Who granted this permission.</summary>
    public string? GrantedBy { get; set; }

    public DateTime GrantedAt { get; set; }
}

/// <summary>Available roles for collection permissions.</summary>
public static class CollectionRoles
{
    public const string Owner = "owner";
    public const string Editor = "editor";
    public const string Viewer = "viewer";

    public static readonly string[] All = { Owner, Editor, Viewer };

    /// <summary>Check if a role can perform write operations.</summary>
    public static bool CanWrite(string role) => role == Owner || role == Editor;

    /// <summary>Check if a role can manage permissions.</summary>
    public static bool CanManage(string role) => role == Owner;
}

// ──── Permission DTOs ────

public class GrantPermissionDto
{
    [Required]
    public string UserEmail { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Role { get; set; } = "viewer";
}

public class UpdatePermissionDto
{
    [Required, MaxLength(20)]
    public string Role { get; set; } = "viewer";
}

public class PermissionInfoDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? UserEmail { get; set; }
    public string? DisplayName { get; set; }
    public string Role { get; set; } = "viewer";
    public DateTime GrantedAt { get; set; }
}
