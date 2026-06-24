using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using LedgerCore.Application.Contracts;

namespace LedgerCore.Application.Features.Accounts.Queries.GetAccountBalance;

public sealed class GetAccountBalanceQueryHandler : IRequestHandler<GetAccountBalanceQuery, decimal>
{
    private readonly IApplicationDbContext _context;

    public GetAccountBalanceQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<decimal> Handle(GetAccountBalanceQuery request, CancellationToken cancellationToken)
    {
        // The core tenet of the ledger: Balance is the sum of all entries.
        var entries = await _context.GetLedgerEntriesForAccountAsync(request.AccountId, cancellationToken);
        var balance = entries.Sum(e => e.Direction == LedgerCore.Domain.Enums.EntryDirection.Credit ? e.Value.Amount : -e.Value.Amount);

        return balance;
    }
}
