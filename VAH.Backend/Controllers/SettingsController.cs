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
public class SettingsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Lấy danh sách cấu hình của người dùng.
    /// CQRS Query pattern - Controller Thin
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSettings(CancellationToken ct = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await mediator.Send(new Features.Settings.Queries.GetSettingsQuery(userId), ct);
        return Ok(result);
    }

    /// <summary>
    /// Cập nhật cấu hình người dùng.
    /// Yêu cầu chỉ nhận DTO không dùng trực tiếp domain entity.
    /// </summary>
    [HttpPatch]
    public async Task<IActionResult> UpdateSettings([FromBody] Features.Settings.Commands.UpdateSettingsCommand command, CancellationToken ct = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        command = command with { UserId = userId }; // Record type mutation
        
        await mediator.Send(command, ct);
        return NoContent();
    }
}
