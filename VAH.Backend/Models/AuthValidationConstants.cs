namespace VAH.Backend.Models;

/// <summary>
/// Shared validation constants for authentication-related DTOs.
/// </summary>
public static class AuthValidationConstants
{
    /// <summary>
    /// Requires 8-100 chars with at least 1 lowercase, 1 uppercase, 1 digit, and 1 special character.
    /// </summary>
    public const string PasswordPolicyRegex =
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\w\s]).{8,100}$";
}
