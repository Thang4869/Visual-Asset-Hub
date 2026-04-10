using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace VAH.Backend.Features.Notifications.Queries;

public record NotificationDto(Guid Id, string Title, string Message, string Type, bool IsRead, DateTime CreatedAtUtc, string? LinkUrl);

public record GetNotificationsQuery(string UserId, int Page, int PageSize) : IRequest<List<NotificationDto>>;

// Query handler cho thông báo (Chỉ lấy DTO)
public class GetNotificationsQueryHandler() : IRequestHandler<GetNotificationsQuery, List<NotificationDto>>
{
    public Task<List<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken ct)
    {
        // Dummy db fetch
        // await _dbContext.Notifications.Where(n => n.UserId == request.UserId).Select(n => ...Dto).ToListAsync(ct);
        
        return Task.FromResult(new List<NotificationDto>
        {
            new NotificationDto(Guid.NewGuid(), "Chào mừng", "Chào mừng bạn tới Visual Asset Hub!", "System", false, DateTime.UtcNow, null)
        });
    }
}
