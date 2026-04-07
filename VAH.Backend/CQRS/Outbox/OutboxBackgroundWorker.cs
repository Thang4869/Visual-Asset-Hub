using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using MediatR;
using VAH.Backend.Data;
using System.Reflection;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VAH.Backend.CQRS.Outbox;

/// <summary>
/// A background service that periodically sweeps the OutboxMessage table and publishes events via MediatR.
/// </summary>
public class OutboxBackgroundWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxBackgroundWorker> _logger;

    public OutboxBackgroundWorker(IServiceProvider serviceProvider, ILogger<OutboxBackgroundWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox background worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing outbox messages.");
            }

            // Polling interval
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var messages = await dbContext.Set<Models.OutboxMessage>()
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(20) // Process in batches
            .ToListAsync(cancellationToken);

        if (!messages.Any())
            return;

        _logger.LogInformation("Processing {Count} outbox messages.", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                var type = Assembly.GetExecutingAssembly().GetType(message.Type);
                if (type == null)
                {
                    message.Error = "Type " + message.Type + " not found.";
                    message.ProcessedOnUtc = DateTime.UtcNow;
                    continue;
                }

                var domainEvent = JsonSerializer.Deserialize(message.Content, type);
                if (domainEvent == null)
                {
                    message.Error = "Failed to deserialize " + message.Type + ".";
                    message.ProcessedOnUtc = DateTime.UtcNow;
                    continue;
                }

                // Publish via MediatR
                await publisher.Publish(domainEvent, cancellationToken);
                message.ProcessedOnUtc = DateTime.UtcNow;
                _logger.LogDebug("Successfully processed outbox message {MessageId} of type {MessageType}", message.Id, message.Type);
            }
            catch (Exception ex)
            {
                message.Error = ex.Message;
                message.ProcessedOnUtc = DateTime.UtcNow;
                _logger.LogError(ex, "Failed to process outbox message {MessageId}", message.Id);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
