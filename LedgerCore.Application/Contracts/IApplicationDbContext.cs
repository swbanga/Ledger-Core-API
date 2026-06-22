using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LedgerCore.Domain.Entities;
using LedgerCore.Domain.Enums;
using LedgerCore.Domain.Projections;

namespace LedgerCore.Application.Contracts;

public interface IApplicationDbContext
{
    Task<Account?> FindAccountAsync(Guid accountId, CancellationToken cancellationToken);
    Task<List<LedgerEntry>> GetLedgerEntriesForAccountAsync(Guid accountId, CancellationToken cancellationToken);
    Task<List<LedgerTransaction>> GetAllTransactionsAsync(CancellationToken cancellationToken);
    Task<AccountBalance?> FindAccountBalanceAsync(Guid accountId, CancellationToken cancellationToken);
    Task AddTransactionAsync(LedgerTransaction transaction, CancellationToken cancellationToken);
    Task AddAccountBalanceAsync(AccountBalance accountBalance, CancellationToken cancellationToken);
    void AddAccount(Account account);
    Task<ITransactionHandle> BeginTransactionAsync(CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public interface ITransactionHandle : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
    Task RollbackAsync(CancellationToken cancellationToken);
}
