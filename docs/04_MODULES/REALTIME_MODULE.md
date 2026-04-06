# Realtime Module

> **Mục đích**: Thông báo thời gian thực đến các client kết nối
> **Last Updated**: 2026-04-06

---

## §1 — Tổng quan (Overview)

| Khía cạnh | Chi tiết |
|-----------|----------|
| **Domain** | Thông báo push thời gian thực đến các client kết nối |
| **Technology** | ASP.NET Core SignalR |
| **Hub** | `AssetHub` → `/hubs/assets` |
| **Service** | `INotificationService` → `NotificationService` |
| **Auth** | JWT qua query string (`?access_token=`) |
| **Patterns** | Observer (hub groups), Mediator (qua IHubContext) |

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

**Thiết kế chính**: Mỗi người dùng được đặt vào một SignalR group đặt tên theo UserId của họ. Điều này cho phép thông báo push có mục tiêu mà không cần broadcast đến tất cả các kết nối.

## §3 — Notification Service

```csharp
public interface INotificationService
{
    Task NotifyAsync(string userId, string eventType, object? payload = null, CancellationToken ct = default);
}
```

Implementation sử dụng `IHubContext<AssetHub>`:

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

## §4 — Các loại Event (Event Types)

| Event | Kích hoạt | Payload |
|-------|-----------|---------|
| `AssetCreated` | Sau upload/create | `AssetResponseDto` |
| `AssetUpdated` | Sau update | `AssetResponseDto` |
| `AssetDeleted` | Sau delete | `{ id }` |
| `AssetsMoved` | Sau bulk move | `{ assetIds, targetCollectionId }` |
| `AssetsDeleted` | Sau bulk delete | `{ assetIds }` |

## §5 — Tích hợp Frontend (Frontend Integration)

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

## §6 — Vòng đời kết nối (Connection Lifecycle)

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
