using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LedgerCore.Domain.Entities;
using LedgerCore.Domain.Enums;

namespace LedgerCore.Application.Contracts;

public interface IApplicationDbContext
{
    Task<Account?> FindAccountAsync(Guid accountId, CancellationToken cancellationToken);
    Task<List<LedgerEntry>> GetLedgerEntriesForAccountAsync(Guid accountId, CancellationToken cancellationToken);
    Task<ITransactionHandle> BeginTransactionAsync(CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public interface ITransactionHandle : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
    Task RollbackAsync(CancellationToken cancellationToken);
}
