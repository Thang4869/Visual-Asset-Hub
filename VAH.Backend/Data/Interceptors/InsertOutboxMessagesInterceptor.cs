using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;
using VAH.Backend.Models;
using VAH.Backend.Models.DomainEvents;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VAH.Backend.Data.Interceptors;

public sealed class InsertOutboxMessagesInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            InsertOutboxMessages(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void InsertOutboxMessages(DbContext context)
    {
        var outboxMessages = context
            .ChangeTracker
            .Entries<IHasDomainEvents>()
            .Select(x => x.Entity)
            .SelectMany(entity =>
            {
                var domainEvents = entity.DomainEvents.ToList();
                entity.ClearDomainEvents();
                return domainEvents;
            })
            .Select(domainEvent => new OutboxMessage
            {
                Id = System.Guid.NewGuid(),
                OccurredOnUtc = System.DateTime.UtcNow,
                Type = domainEvent.GetType().FullName ?? domainEvent.GetType().Name,
                Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType())
            })
            .ToList();

        context.Set<OutboxMessage>().AddRange(outboxMessages);
    }
}
