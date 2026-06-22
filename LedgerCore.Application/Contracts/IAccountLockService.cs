using System;
using System.Threading;
using System.Threading.Tasks;

namespace LedgerCore.Application.Contracts;

/// <summary>
/// Acquires an exclusive row-level lock on an account within the current database transaction.
/// </summary>
public interface IAccountLockService
{
    Task AcquireRowLockAsync(Guid accountId, CancellationToken cancellationToken = default);
}
