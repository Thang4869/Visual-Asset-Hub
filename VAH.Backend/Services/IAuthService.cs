using VAH.Backend.Models;

namespace VAH.Backend.Services;

/// <summary>
/// Authentication service for user registration and login.
/// </summary>
public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
}
