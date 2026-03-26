# REALTIME MODULE

> **Last Updated**: 2026-03-02
> **Status**: Active — Hubs/ layer
> **Last Updated**: 2026-03-26
> **Status**: Active — Services/ layer
> **Note**: Some domain entities still use DataAnnotations in code. Prefer Fluent API migrations as documented in Architecture Conventions.
---

## §1 — Overview

| Aspect | Detail |
|--------|--------|
| **Domain** | Real-time push notifications to connected clients |
| **Technology** | ASP.NET Core SignalR |
| **Hub** | `AssetHub` → `/hubs/assets` |
| **Service** | `INotificationService` → `NotificationService` |
| **Auth** | JWT via query string (`?access_token=`) |
| **Patterns** | Observer (hub groups), Mediator (via IHubContext) |

## §2 — Hub Implementation

```csharp
[Authorize]
public class AssetHub : Hub
{
    // User joins their personal group on connect
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != null)
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        await base.OnConnectedAsync();
    }

    // User leaves group on disconnect
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != null)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
        await base.OnDisconnectedAsync(exception);
    }
}
```

**Key Design**: Each user is placed in a SignalR group named by their UserId. This enables targeted push notifications without broadcasting to all connections.

## §3 — Notification Service

```csharp
public interface INotificationService
{
    Task NotifyAsync(string userId, string eventType, object? payload = null, CancellationToken ct = default);
}
```

Implementation uses `IHubContext<AssetHub>`:

```csharp
public class NotificationService : INotificationService
{
    private readonly IHubContext<AssetHub> _hubContext;

    public async Task NotifyAsync(string userId, string eventType, object? payload, CancellationToken ct)
    {
        await _hubContext.Clients.Group(userId).SendAsync(eventType, payload, ct);
    }
}
```

## §4 — Event Types

| Event | Trigger | Payload |
|-------|---------|---------|
| `AssetCreated` | After upload/create | `AssetResponseDto` |
| `AssetUpdated` | After update | `AssetResponseDto` |
| `AssetDeleted` | After delete | `{ id }` |
| `AssetsMoved` | After bulk move | `{ assetIds, targetCollectionId }` |
| `AssetsDeleted` | After bulk delete | `{ assetIds }` |

## §5 — Frontend Integration

```javascript
// useSignalR hook
const connection = new signalR.HubConnectionBuilder()
    .withUrl(`${baseUrl}/hubs/assets`, {
        accessTokenFactory: () => TokenManager.getToken()
    })
    .withAutomaticReconnect()
    .build();

connection.on("AssetCreated", (asset) => { /* update local state */ });
connection.on("AssetDeleted", ({ id }) => { /* remove from state */ });
```

## §6 — Connection Lifecycle

```
Browser                          AssetHub                    SignalR Groups
  │                                  │                            │
  │── Connect (JWT query) ──────────→│                            │
  │                                  │── AddToGroup(userId) ─────→│
  │←── Connected ────────────────────│                            │
  │                                  │                            │
  │   ... (asset operations) ...     │                            │
  │                                  │                            │
  │←── SendAsync("AssetCreated") ────│←── via Group(userId) ──────│
  │                                  │                            │
  │── Disconnect ───────────────────→│                            │
  │                                  │── RemoveFromGroup ────────→│
```

---

> **Document End**
