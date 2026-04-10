using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace VAH.Backend.Features.Settings.Queries;

public record SettingsDto(string Theme, string LayoutType, bool ReceiveEmailNotifications);

public record GetSettingsQuery(string UserId) : IRequest<SettingsDto>;

// Giả lập QueryHandler giao tiếp DB
public class GetSettingsQueryHandler() : IRequestHandler<GetSettingsQuery, SettingsDto>
{
    // Cần inject DbContext theo chuẩn, ở đây tôi tạo dummy. Nên query bằng DTO projection (Select)
    public Task<SettingsDto> Handle(GetSettingsQuery request, CancellationToken ct)
    {
        // Thực tế sẽ là: return await _dbContext.UserSettings.Where(s => s.UserId == request.UserId).Select(s => new SettingsDto(...)).FirstOrDefaultAsync(ct);
        return Task.FromResult(new SettingsDto("light", "grid", true)); 
    }
}
