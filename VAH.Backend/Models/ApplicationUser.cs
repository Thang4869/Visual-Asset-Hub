using Microsoft.AspNetCore.Identity;

namespace VAH.Backend.Models;

/// <summary>
/// Application user entity extending ASP.NET Identity.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
