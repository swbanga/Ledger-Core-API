using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LedgerCore.Application.Contracts;
using LedgerCore.Application.Features.Admin.Commands.RebuildProjections;
using LedgerCore.Domain.Enums;
using LedgerCore.Domain.ReadModels;
using LedgerCore.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Infrastructure.Services;

public class ProjectionRebuildService : IProjectionRebuildService
{
    private readonly LedgerDbContext _context;

    public ProjectionRebuildService(LedgerDbContext context)
    {
        _context = context;
    }

    public async Task<RebuildProjectionsResult> RebuildAsync(CancellationToken cancellationToken)
    {
        var balanceStates = _context.Set<AccountBalanceState>();

        // Clear existing read‑model
        var existing = await balanceStates.ToListAsync(cancellationToken);
        balanceStates.RemoveRange(existing);

        // Load all posted transactions in deterministic order
        var transactionsQuery = _context.Set<LedgerCore.Domain.Entities.LedgerTransaction>()
            .Include(t => t.Entries)
            .Where(t => t.Status == TransactionStatus.Posted)
            .OrderBy(t => t.TimestampUtc)
            .ThenBy(t => t.Id)
            .AsAsyncEnumerable();

        var balances = new Dictionary<Guid, decimal>();
        int txCount = 0;

        await foreach (var tx in transactionsQuery.WithCancellation(cancellationToken))
        {
            txCount++;
            foreach (var entry in tx.Entries)
            {
                // Credit increases the account balance, debit decreases it.
                var signedAmount = entry.Direction == EntryDirection.Credit
                    ? entry.Value.Amount
                    : -entry.Value.Amount;

                balances.TryGetValue(entry.AccountId, out var current);
                balances[entry.AccountId] = current + signedAmount;
            }
        }

        // Persist rebuilt balances
        foreach (var kv in balances)
        {
            var state = new AccountBalanceState(kv.Key)
            {
                CurrentBalance = kv.Value,
                LastUpdatedUtc = DateTime.UtcNow
            };
            balanceStates.Add(state);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new RebuildProjectionsResult(balances.Count, txCount);
    }
}
