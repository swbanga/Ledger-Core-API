using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
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
        var balance = await _context.LedgerEntries
            .Where(e => e.AccountId == request.AccountId)
            .SumAsync(e => e.Value.Amount, cancellationToken);

        return balance;
    }
}
