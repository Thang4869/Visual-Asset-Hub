# Auth Module

> **Mục đích**: Xác thực người dùng và quản lý JWT token
> **Last Updated**: 2026-04-06

---

## §1 — Tổng quan (Overview)

| Khía cạnh | Chi tiết |
|-----------|----------|
| **Domain** | Đăng ký người dùng, đăng nhập, cấp JWT |
| **Entity** | `ApplicationUser` (kế thừa `IdentityUser`) |
| **Service** | `IAuthService` → `AuthService` |
| **Controller** | `AuthController` (2 endpoints, rate-limited) — `Register` trả về 201 Created |
| **Identity Provider** | ASP.NET Identity với EF Core stores |
| **Patterns** | Facade pattern cho Identity + JWT generation |

## §2 — Domain Model

```csharp
public class ApplicationUser : IdentityUser
{
    string DisplayName       // User's display name
    DateTime CreatedAt       // Registration timestamp (UTC)
}
```

Kế thừa `IdentityUser` cung cấp: Id (GUID string), Email, PasswordHash, UserName, v.v.

## §3 — Service Interface

```csharp
public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct);
    Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct);
}
```

**DTOs:**

```csharp
public class RegisterDto { string Email, string Password, string DisplayName }
public class LoginDto { string Email, string Password }
public class AuthResponseDto { string Token, string Email, string DisplayName, DateTime ExpiresAt }
```

## §4 — API Endpoints

| Method | Route | Rate Limit | Mô tả |
|--------|-------|-----------|-------|
| POST | `/api/v1/auth/register` | Fixed (100/min) | Tạo tài khoản + tự động tạo collection mặc định (201 Created) |
| POST | `/api/v1/auth/login` | Fixed (100/min) | Xác thực + trả về JWT (200 OK) |

## §5 — Luồng xác thực (Authentication Flow)

```
Client                AuthController    IAuthService    UserManager    JwtGenerator
  │                       │                │               │              │
  │── POST /auth/login ──→│                │               │              │
  │   {email, password}   │                │               │              │
  │                       │── LoginAsync ─→│               │              │
  │                       │                │── FindByEmail→│              │
  │                       │                │←── user ──────│              │
  │                       │                │── CheckPwd ──→│              │
  │                       │                │←── valid ─────│              │
  │                       │                │── Generate ─────────────────→│
  │                       │                │←── JWT token ────────────────│
  │                       │←── AuthResponseDto ────────────│              │
  │←── 200 { token, ... }─│                │               │              │
```

## §6 — Tác động phụ khi đăng ký (Registration Side Effects)

Khi người dùng đăng ký thành công, `AuthService` thực hiện:
1. Tạo `ApplicationUser` qua `UserManager.CreateAsync()`
2. Gọi `ICollectionService.CreateAsync()` để tạo collection mặc định "My Collection" cho người dùng mới
3. Trả về JWT token ngay lập tức (tự động đăng nhập sau khi đăng ký)

## §7 — Cấu hình Identity (Identity Configuration)

| Cài đặt | Giá trị |
|---------|---------|
| Độ dài mật khẩu tối thiểu | 6 |
| Yêu cầu chữ số | Có |
| Yêu cầu chữ thường | Có |
| Yêu cầu chữ hoa | Không |
| Yêu cầu ký tự đặc biệt | Không |
| Email duy nhất | Có |

## §8 — Cấu hình JWT (JWT Configuration)

| Tham số | Nguồn | Ghi chú |
|---------|-------|---------|
| SecretKey | `Jwt:SecretKey` | ≥ 256-bit, bắt buộc |
| Issuer | `Jwt:Issuer` | Token issuer claim |
| Audience | `Jwt:Audience` | Token audience claim |
| ClockSkew | `TimeSpan.Zero` | Hết hạn chính xác |
| SignalR | Query string `access_token` | Cho WebSocket auth |

---

> **Document End**
