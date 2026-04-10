using System;

namespace VAH.Backend.Models;

/// <summary>
/// Domain Entity đại diện cho một thông báo hệ thống hoặc tin nhắn ứng dụng đến người dùng cụ thể.
/// </summary>
public class Notification
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; }
    public string Title { get; private set; }
    public string Message { get; private set; }
    public string Type { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public string? LinkUrl { get; private set; }

    // EF Core constructor
    private Notification() 
    {
        UserId = string.Empty;
        Title = string.Empty;
        Message = string.Empty;
        Type = string.Empty;
    }

    public Notification(string userId, string title, string message, string type, string? linkUrl = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Title = title;
        Message = message;
        Type = type;
        LinkUrl = linkUrl;
        CreatedAtUtc = DateTime.UtcNow;
        IsRead = false;
    }

    /// <summary>
    /// Đánh dấu là đã đọc
    /// </summary>
    public void MarkAsRead()
    {
        if (IsRead) return;
        IsRead = true;
    }
}
