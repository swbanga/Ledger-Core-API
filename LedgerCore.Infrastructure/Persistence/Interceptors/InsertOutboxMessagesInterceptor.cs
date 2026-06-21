using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LedgerCore.Domain.Entities;
using LedgerCore.Domain.Events;
using LedgerCore.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LedgerCore.Infrastructure.Persistence.Interceptors;

public sealed class InsertOutboxMessagesInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null)
            return new ValueTask<InterceptionResult<int>>(result);

        ProcessOutboxMessages(context);

        return new ValueTask<InterceptionResult<int>>(result);
    }

    private static void ProcessOutboxMessages(DbContext context)
    {
        var domainEvents = context.ChangeTracker.Entries()
            .SelectMany(entry =>
            {
                var method = entry.Entity.GetType().GetMethod(nameof(LedgerTransaction.GetDomainEvents));
                if (method == null)
                    return Enumerable.Empty<(EntityEntry Entry, IDomainEvent Event)>();

                var events = method.Invoke(entry.Entity, null) as IReadOnlyCollection<IDomainEvent>;
                if (events == null)
                    return Enumerable.Empty<(EntityEntry, IDomainEvent)>();

                return events.Select(e => (entry, e));
            })
            .ToList();

        foreach (var (entityEntry, domainEvent) in domainEvents)
        {
            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                OccurredOn = DateTime.UtcNow,
                Type = domainEvent.GetType().AssemblyQualifiedName!,
                Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), new JsonSerializerOptions())
            };

            context.Add(outboxMessage);
        }
    }
}
