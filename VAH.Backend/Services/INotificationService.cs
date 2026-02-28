namespace VAH.Backend.Services;

/// <summary>
/// Sends real-time notifications to connected clients via SignalR.
/// </summary>
public interface INotificationService
{
    Task NotifyAsync(string userId, string eventType, object? payload = null, CancellationToken ct = default);
}
