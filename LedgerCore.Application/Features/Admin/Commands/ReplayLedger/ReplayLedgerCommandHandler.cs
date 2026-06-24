using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LedgerCore.Application.Contracts;
using LedgerCore.Domain.Entities;
using MediatR;

namespace LedgerCore.Application.Features.Admin.Commands.ReplayLedger;

public sealed class ReplayLedgerCommandHandler : IRequestHandler<ReplayLedgerCommand, ReplayResult>
{
    private readonly IApplicationDbContext _dbContext;

    public ReplayLedgerCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<ReplayResult> Handle(ReplayLedgerCommand request, CancellationToken cancellationToken)
    {
        List<LedgerTransaction> transactions = await _dbContext.GetAllTransactionsAsync(cancellationToken);

        int total = transactions.Count;
        var corruptedIds = new List<Guid>(capacity: total);

        foreach (LedgerTransaction txn in transactions)
        {
            // No entries means sum is 0, which passes the invariant.
            decimal sum = txn.Entries.Sum(e => e.Direction == LedgerCore.Domain.Enums.EntryDirection.Credit ? e.Value.Amount : -e.Value.Amount);
            if (sum != 0m)
            {
                corruptedIds.Add(txn.Id);
            }
        }

        bool mathematicallyValid = corruptedIds.Count == 0;

        return new ReplayResult(
            TotalTransactionsProcessed: total,
            IsMathematicallyValid: mathematicallyValid,
            CorruptedTransactionIds: corruptedIds);
    }
}
