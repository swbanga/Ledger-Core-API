using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using LedgerCore.Domain.Events;
using LedgerCore.Domain.Entities;
using LedgerCore.Domain.Enums;
using LedgerCore.Domain.ReadModels;
using LedgerCore.Infrastructure.Database;

namespace LedgerCore.Application.Features.Projections;

public sealed class BalanceProjectionConsumer : IConsumer<TransactionPostedDomainEvent>
{
    private readonly LedgerDbContext _dbContext;

    public BalanceProjectionConsumer(LedgerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<TransactionPostedDomainEvent> context)
    {
        var message = context.Message;
        var transactionId = message.TransactionId;
        var entries = message.Entries;

        // Group net delta per account (Credit adds, Debit subtracts)
        var accountDeltas = new Dictionary<Guid, decimal>();
        foreach (var entry in entries)
        {
            decimal amount = entry.Value.Amount;
            // Determine sign based on entry direction
            decimal signedAmount = entry.Direction == EntryDirection.Credit ? amount : -amount;
            if (accountDeltas.ContainsKey(entry.AccountId))
                accountDeltas[entry.AccountId] += signedAmount;
            else
                accountDeltas[entry.AccountId] = signedAmount;
        }

        var updatedAccountIds = new HashSet<Guid>();
        foreach (var kvp in accountDeltas)
        {
            var accountId = kvp.Key;
            var delta = kvp.Value;

            var accountState = await _dbContext.AccountBalanceStates
                .SingleOrDefaultAsync(x => x.AccountId == accountId, context.CancellationToken);

            bool alreadyProcessed = accountState != null && accountState.LastTransactionId == transactionId;
            if (alreadyProcessed)
                continue;

            if (accountState == null)
            {
                accountState = new AccountBalanceState(accountId)
                {
                    CurrentBalance = 0,
                    LastTransactionId = Guid.Empty,
                    LastUpdatedUtc = DateTime.UtcNow
                };
                _dbContext.AccountBalanceStates.Add(accountState);
            }

            accountState.CurrentBalance += delta;
            accountState.LastTransactionId = transactionId;
            accountState.LastUpdatedUtc = DateTime.UtcNow;

            updatedAccountIds.Add(accountId);
        }

        if (updatedAccountIds.Count > 0)
            await _dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
