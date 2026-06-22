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
        var transactions = await _context.Set<LedgerCore.Domain.Entities.LedgerTransaction>()
            .Include(t => t.Entries)
            .Where(t => t.Status == TransactionStatus.Posted)
            .OrderBy(t => t.TimestampUtc)
            .ThenBy(t => t.Id)
            .ToListAsync(cancellationToken);

        var balances = new Dictionary<Guid, decimal>();

        foreach (var tx in transactions)
        {
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
            var state = new AccountBalanceState
            {
                AccountId = kv.Key,
                Balance = kv.Value,
                // Currency, LastUpdated etc. can be filled when the read model requires them.
            };
            balanceStates.Add(state);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new RebuildProjectionsResult(balances.Count, transactions.Count);
    }
}
