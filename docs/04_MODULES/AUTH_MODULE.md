# AUTH MODULE

> **Last Updated**: 2026-03-02
> **Status**: Active — Services/ layer

---

## §1 — Overview

| Aspect | Detail |
|--------|--------|
| **Domain** | User registration, login, JWT issuance |
| **Entity** | `ApplicationUser` (extends `IdentityUser`) |
| **Service** | `IAuthService` → `AuthService` |
| **Controller** | `AuthController` (2 endpoints, rate-limited) |
| **Identity Provider** | ASP.NET Identity with EF Core stores |
| **Patterns** | Facade over Identity + JWT generation |

## §2 — Domain Model

```csharp
public class ApplicationUser : IdentityUser
{
    string DisplayName       // User's display name
    DateTime CreatedAt       // Registration timestamp (UTC)
}
```

Extends `IdentityUser` which provides: Id (GUID string), Email, PasswordHash, UserName, etc.

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

| Method | Route | Rate Limit | Description |
|--------|-------|-----------|-------------|
| POST | `/api/v1/auth/register` | Fixed (100/min) | Create account + auto-create default collection |
| POST | `/api/v1/auth/login` | Fixed (100/min) | Authenticate + return JWT |

## §5 — Authentication Flow

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

## §6 — Registration Side Effects

When a user registers successfully, `AuthService` performs:
1. Creates `ApplicationUser` via `UserManager.CreateAsync()`
2. Calls `ICollectionService.CreateAsync()` to create a default "My Collection" for the new user
3. Returns JWT token immediately (auto-login after registration)

## §7 — Identity Configuration

| Setting | Value |
|---------|-------|
| Password Min Length | 6 |
| Require Digit | Yes |
| Require Lowercase | Yes |
| Require Uppercase | No |
| Require Special Char | No |
| Unique Email | Yes |

## §8 — JWT Configuration

| Parameter | Source | Notes |
|-----------|--------|-------|
| SecretKey | `Jwt:SecretKey` | ≥ 256-bit, mandatory |
| Issuer | `Jwt:Issuer` | Token issuer claim |
| Audience | `Jwt:Audience` | Token audience claim |
| ClockSkew | `TimeSpan.Zero` | Strict expiration |
| SignalR | Query string `access_token` | For WebSocket auth |

---

> **Document End**
