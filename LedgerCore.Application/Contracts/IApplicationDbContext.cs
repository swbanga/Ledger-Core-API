using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LedgerCore.Domain.Entities;
using LedgerCore.Domain.Enums;
using LedgerCore.Domain.Projections;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Application.Contracts;

public interface IApplicationDbContext
{
    DbSet<Account> Accounts { get; }
    DbSet<LedgerTransaction> LedgerTransactions { get; }
    DbSet<LedgerEntry> LedgerEntries { get; }
    DbSet<AccountBalance> AccountBalances { get; }
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
