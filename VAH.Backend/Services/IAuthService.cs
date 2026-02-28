using VAH.Backend.Models;

namespace VAH.Backend.Services;

/// <summary>
/// Authentication service for user registration and login.
/// </summary>
public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default);
    Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default);
}
