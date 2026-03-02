# ADR-005: JWT Authentication with SignalR Support

> **Status**: Accepted
> **Date**: 2026-02-25
> **Deciders**: Tech Lead

## Context

The application requires stateless authentication for the REST API and real-time bidirectional communication via SignalR. SignalR WebSocket connections cannot send custom HTTP headers after the initial handshake, so the standard `Authorization: Bearer <token>` header approach doesn't work for persistent connections.

## Decision

Use **JWT Bearer tokens** with a **SignalR query string fallback**:

### REST API Authentication
Standard JWT in `Authorization` header:
```
GET /api/v1/assets HTTP/1.1
Authorization: Bearer eyJhbG...
```

### SignalR Authentication
Token passed via query string on connection establishment:
```
wss://host/hubs/assets?access_token=eyJhbG...
```

Handled in `JwtBearerEvents.OnMessageReceived`:
```csharp
OnMessageReceived = context =>
{
    var accessToken = context.Request.Query["access_token"];
    var path = context.HttpContext.Request.Path;
    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
    {
        context.Token = accessToken;
    }
    return Task.CompletedTask;
};
```

### Identity Configuration
- ASP.NET Identity with `ApplicationUser` (extends `IdentityUser` + DisplayName + CreatedAt)
- Password: min 6 chars, require digit + lowercase
- Unique email enforced
- JWT: `ClockSkew = TimeSpan.Zero` (strict expiration)

### Authorization Policies
- `RequireAssetRead` → `RequireAuthenticatedUser()`
- `RequireAssetWrite` → `RequireAuthenticatedUser()`

(Currently both resolve to authenticated-only; role-based filtering planned.)

### Frontend Token Management
- `TokenManager` singleton with private `#storageKey` field
- Axios interceptor auto-attaches Bearer header
- Token refresh not yet implemented (see Technical Debt)

## Consequences

### Positive
- Stateless: no server-side session storage needed
- Single auth mechanism for both REST and WebSocket
- ASP.NET Identity handles password hashing, lockout, email uniqueness

### Negative
- JWT in query string appears in server logs (must be filtered in production Serilog config)
- No token refresh — user must re-login on expiry
- `ClockSkew = Zero` means any server time drift causes auth failures

### Neutral
- CORS must allow credentials for SignalR (`AllowCredentials()`)
- Rate limiting applies to auth endpoints (Fixed: 100/min)

## Compliance

- All API endpoints (except `/api/v1/health`) must require `[Authorize]`
- SignalR hub must validate connection via `OnConnectedAsync` user identity
- JWT secret must be ≥ 256-bit and stored in environment variables (not in appsettings for production)

---

> **Document End**
