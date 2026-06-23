using System;
using System.Threading;
using System.Threading.Tasks;
using LedgerCore.Application.Contracts;
using LedgerCore.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Infrastructure.Locking;

/// <summary>
/// Infrastructure implementation that uses <c>UPDLOCK, ROWLOCK</c> hints
/// to serialise access to the source account row.
/// </summary>
public sealed class AccountLockService : IAccountLockService
{
    private readonly LedgerDbContext _context;

    public AccountLockService(LedgerDbContext context)
    {
        _context = context;
    }

    public async Task AcquireRowLockAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        // The hint forces SQL Server to take an update lock on the row
        // while the transaction is active.
        await _context.Database.ExecuteSqlRawAsync(
            "SELECT 1 FROM [Accounts] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {0}",
            new object[] { accountId }, cancellationToken);
    }
}
