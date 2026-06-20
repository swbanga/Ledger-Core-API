using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LedgerCore.Domain.Events;
using LedgerCore.Domain.Entities;
using LedgerCore.Domain.Projections;
using LedgerCore.Application.Data; // Interface for your DbContext

namespace LedgerCore.Application.Features.Projections;

public class AccountBalanceProjector : INotificationHandler<TransactionPostedDomainEvent>
{
    private readonly LedgerCore.Application.Data.IApplicationDbContext _context;

    public AccountBalanceProjector(LedgerCore.Application.Data.IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(TransactionPostedDomainEvent notification, CancellationToken cancellationToken)
    {
        foreach (var entry in notification.Entries)
        {
            var projection = await _context.AccountBalances
                .FirstOrDefaultAsync(b => b.AccountId == entry.AccountId, cancellationToken);

            if (projection == null)
            {
                projection = new AccountBalance
                {
                    AccountId = entry.AccountId,
                    CurrentBalance = entry.Amount,
                    LastUpdatedAt = DateTimeOffset.UtcNow
                };
                _context.AccountBalances.Add(projection);
            }
            else
            {
                projection.CurrentBalance += entry.Amount;
                projection.LastUpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
