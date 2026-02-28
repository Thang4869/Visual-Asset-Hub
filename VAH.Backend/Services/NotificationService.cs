using Microsoft.AspNetCore.SignalR;
using VAH.Backend.Hubs;

namespace VAH.Backend.Services;

public class NotificationService : INotificationService
{
    private readonly IHubContext<AssetHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IHubContext<AssetHub> hubContext, ILogger<NotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyAsync(string userId, string eventType, object? payload = null, CancellationToken ct = default)
    {
        try
        {
            await _hubContext.Clients.Group($"user:{userId}")
                .SendAsync(eventType, payload, ct);

            _logger.LogDebug("SignalR notification sent: {Event} to user {UserId}", eventType, userId);
        }
        catch (Exception ex)
        {
            // Never fail the parent operation because of notification failure
            _logger.LogWarning(ex, "Failed to send SignalR notification {Event} to user {UserId}", eventType, userId);
        }
    }
}
