using MediatR;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VAH.Backend.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class NotificationsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Lấy danh sách thông báo và chia trang phân trang.
    /// Thin Controller SRP. (Lưu ý: API chỉ trả DTO).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await mediator.Send(new Features.Notifications.Queries.GetNotificationsQuery(userId, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// Đánh dấu một thông báo thành "Đã Đọc".
    /// </summary>
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(System.Guid id, CancellationToken ct = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await mediator.Send(new Features.Notifications.Commands.MarkNotificationAsReadCommand(id, userId), ct);
        return NoContent();
    }
}
