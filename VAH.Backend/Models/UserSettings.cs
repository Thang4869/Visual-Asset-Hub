using System;

namespace VAH.Backend.Models;

/// <summary>
/// Domain Entity cho cấu hình cài đặt của người dùng.
/// Áp dụng tính Encapsulation (ENC-01): Các thuộc tính private set; chỉ thay đổi qua Behavior Methods.
/// </summary>
public class UserSettings
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string Theme { get; private set; } = "light";
    public string LayoutType { get; private set; } = "grid";
    public bool ReceiveEmailNotifications { get; private set; } = true;

    // EF Core constructor
    private UserSettings() { }

    public UserSettings(string userId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
    }

    /// <summary>
    /// Thay đổi giao diện hiển thị
    /// </summary>
    public void ChangeTheme(string newTheme)
    {
        if (string.IsNullOrWhiteSpace(newTheme))
            throw new ArgumentException("Theme cannot be empty.");
            
        Theme = newTheme;
    }

    /// <summary>
    /// Cập nhật bố cục hệ thống
    /// </summary>
    public void ChangeLayout(string newLayout)
    {
        if (newLayout != "grid" && newLayout != "list" && newLayout != "canvas")
            throw new ArgumentException("Invalid layout type.");

        LayoutType = newLayout;
    }

    /// <summary>
    /// Cập nhật tuỳ chọn nhận email
    /// </summary>
    public void ToggleEmailNotifications(bool enable)
    {
        ReceiveEmailNotifications = enable;
    }
}
