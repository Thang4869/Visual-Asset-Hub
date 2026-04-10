using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace VAH.Backend.Features.Notifications.Commands;

/// <summary>
/// Mark notification as read (CQRS Command)
/// </summary>
public record MarkNotificationAsReadCommand(Guid NotificationId, string UserId) : IRequest;

public class MarkNotificationAsReadCommandHandler() : IRequestHandler<MarkNotificationAsReadCommand>
{
    // Inject _dbContext
    public async Task Handle(MarkNotificationAsReadCommand request, CancellationToken ct)
    {
        // code
        // var notif = await _dbContext.Notifications.FirstOrDefaultAsync(n => n.Id == request.NotificationId && n.UserId == request.UserId, ct);
        // if (notif != null) 
        //     notif.MarkAsRead();  (Dùng method thay vì `IsRead = true` theo chuẩn Encapsulation)
        
        await Task.CompletedTask;
    }
}
