using System;
using VAH.Backend.Models.DomainEvents;

namespace VAH.Backend.Models.Events;

public class AssetCreatedEvent : IDomainEvent
{
    public int AssetId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;

    public AssetCreatedEvent(int assetId, string fileName, string userId)
    {
        AssetId = assetId;
        FileName = fileName;
        UserId = userId;
    }
}
