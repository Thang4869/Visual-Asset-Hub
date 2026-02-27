using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace VAH.Backend.Hubs;

/// <summary>
/// Real-time hub for asset and collection change notifications.
/// Clients join a group based on their UserId so they only receive their own updates.
/// </summary>
[Authorize]
public class AssetHub : Hub
{
    private readonly ILogger<AssetHub> _logger;

    public AssetHub(ILogger<AssetHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
            _logger.LogInformation("SignalR: User {UserId} connected (ConnectionId={ConnectionId})", userId, Context.ConnectionId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{userId}");
            _logger.LogInformation("SignalR: User {UserId} disconnected", userId);
        }
        await base.OnDisconnectedAsync(exception);
    }
}

/// <summary>
/// Event types sent to clients via SignalR.
/// </summary>
public static class HubEvents
{
    public const string AssetCreated = "AssetCreated";
    public const string AssetUpdated = "AssetUpdated";
    public const string AssetDeleted = "AssetDeleted";
    public const string AssetsUploaded = "AssetsUploaded";
    public const string AssetsBulkDeleted = "AssetsBulkDeleted";
    public const string AssetsBulkMoved = "AssetsBulkMoved";
    public const string CollectionCreated = "CollectionCreated";
    public const string CollectionUpdated = "CollectionUpdated";
    public const string CollectionDeleted = "CollectionDeleted";
    public const string TagsChanged = "TagsChanged";
}
