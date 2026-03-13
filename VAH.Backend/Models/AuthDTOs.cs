using System.ComponentModel.DataAnnotations;

namespace VAH.Backend.Models;

public sealed record RegisterDto
{
    [Required(ErrorMessage = "Vui lòng nhập tên hiển thị.")]
    [MaxLength(100, ErrorMessage = "Tên hiển thị không được vượt quá 100 ký tự.")]
    public string DisplayName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [MaxLength(256, ErrorMessage = "Email không được vượt quá 256 ký tự.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự.")]
    [MaxLength(100, ErrorMessage = "Mật khẩu không được vượt quá 100 ký tự.")]
    [RegularExpression(
        AuthValidationConstants.PasswordPolicyRegex,
        ErrorMessage = "Mật khẩu phải bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.")]
    public string Password { get; init; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu.")]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu nhập lại không khớp.")]
    public string ConfirmPassword { get; init; } = string.Empty;
}

public sealed record LoginDto
{
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    public string Password { get; init; } = string.Empty;
}

public sealed record AuthResponseDto
{
    public string Token { get; init; } = string.Empty;
    public DateTime Expiration { get; init; }
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime? RefreshTokenExpiration { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
}
