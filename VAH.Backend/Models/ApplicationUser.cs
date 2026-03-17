using Microsoft.AspNetCore.Identity;

namespace VAH.Backend.Models;

/// <summary>
/// Application user entity extending ASP.NET Identity.
/// </summary>
public class ApplicationUser : IdentityUser
{
    // Parameterless constructor for EF Core / Identity materialization
    protected ApplicationUser() { }

    // Domain ctor for creating new users in application code
    public ApplicationUser(string displayName)
    {
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? string.Empty : displayName.Trim();
        CreatedAt = DateTime.UtcNow;
    }

    // Immutable-ish properties: private setters so mutations go through domain methods
    public string DisplayName { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Optional audit for user profile updates
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Domain method to update the display name with validation and audit.
    /// </summary>
    public void SetDisplayName(string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        var trimmed = displayName.Trim();
        if (DisplayName == trimmed) return;

        DisplayName = trimmed;
        UpdatedAt = DateTime.UtcNow;
    }
}
