using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace VAH.Backend.Features.Settings.Commands;

/// <summary>
/// Command cập nhật cấu hình cá nhân. Lưu ý `UserId` sẽ được Controller ghi đè từ Claims.
/// </summary>
public record UpdateSettingsCommand(string? UserId, string? Theme, string? LayoutType, bool? ReceiveEmailNotifications) : IRequest;

public class UpdateSettingsCommandHandler() : IRequestHandler<UpdateSettingsCommand>
{
    // Inject DbContext vào đây.
    public async Task Handle(UpdateSettingsCommand request, CancellationToken ct)
    {
        // 1. var settings = await _dbContext.UserSettings.FirstOrDefaultAsync(s => s.UserId == request.UserId, ct);
        // 2. if (settings == null) ... 
        
        // 3. Encapsulation: Gọi domain methods.
        // if (!string.IsNullOrEmpty(request.Theme))
        //     settings.ChangeTheme(request.Theme);
        //
        // if (!string.IsNullOrEmpty(request.LayoutType))
        //     settings.ChangeLayout(request.LayoutType);
            
        // 4. await _dbContext.SaveChangesAsync(ct);
        
        await Task.CompletedTask; // Simulated success
    }
}
