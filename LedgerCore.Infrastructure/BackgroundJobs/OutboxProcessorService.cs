using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using LedgerCore.Infrastructure.Database;
using LedgerCore.Infrastructure.Outbox;

namespace LedgerCore.Infrastructure.BackgroundJobs;

public sealed class OutboxProcessorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessorService> _logger;

    public OutboxProcessorService(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox processor started.");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessOutboxAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in outbox processing loop");
                // Delay a bit before next attempt to avoid tight error loops
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _logger.LogInformation("Outbox processor stopped.");
    }

    private async Task ProcessOutboxAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var messages = await dbContext.Set<OutboxMessage>()
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOn)
            .Take(20)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
        {
            return;
        }

        _logger.LogDebug("Found {Count} pending outbox messages", messages.Count);

        foreach (var msg in messages)
        {
            try
            {
                var type = Type.GetType(msg.Type);
                if (type == null)
                {
                    _logger.LogWarning("Unknown event type {Type} for outbox message {Id}", msg.Type, msg.Id);
                    continue;
                }

                var notification = JsonSerializer.Deserialize(msg.Content, type) as INotification;
                if (notification == null)
                {
                    _logger.LogWarning("Failed to deserialize outbox message {Id}", msg.Id);
                    continue;
                }

                await publisher.Publish(notification, cancellationToken);
                msg.ProcessedOnUtc = DateTime.UtcNow;
                _logger.LogDebug("Processed outbox message {Id} of type {Type}", msg.Id, type.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox message {Id}", msg.Id);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
