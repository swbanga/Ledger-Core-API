using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using LedgerCore.Domain.Events;
using LedgerCore.Domain.Entities;
using LedgerCore.Domain.Projections;
using LedgerCore.Application.Contracts;

namespace LedgerCore.Application.Features.Projections;

public class AccountBalanceProjector : INotificationHandler<DomainEventNotification<TransactionPostedDomainEvent>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDistributedCache _cache;

    public AccountBalanceProjector(IApplicationDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task Handle(DomainEventNotification<TransactionPostedDomainEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var balances = new Dictionary<Guid, AccountBalance>();

        foreach (var entry in domainEvent.Entries)
        {
            var delta = entry.Direction == Domain.Enums.EntryDirection.Credit
                            ? entry.Value.Amount
                            : -entry.Value.Amount;

            if (!balances.TryGetValue(entry.AccountId, out var projection))
            {
                projection = await _context.FindAccountBalanceAsync(entry.AccountId, cancellationToken);

                if (projection == null)
                {
                    projection = new AccountBalance
                    {
                        AccountId = entry.AccountId,
                        CurrentBalance = 0,
                        LastUpdatedAt = DateTimeOffset.UtcNow
                    };
                    await _context.AddAccountBalanceAsync(projection, cancellationToken);
                }

                balances[entry.AccountId] = projection;
            }

            projection.CurrentBalance += delta;
            projection.LastUpdatedAt = DateTimeOffset.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        foreach (var kvp in balances)
        {
            var projection = kvp.Value;
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };
            await _cache.SetStringAsync(
                $"AccountBalance_{projection.AccountId}",
                JsonSerializer.Serialize(projection),
                cacheOptions,
                cancellationToken);
        }
    }
}
