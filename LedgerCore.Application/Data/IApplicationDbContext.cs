using LedgerCore.Domain.Entities;
using LedgerCore.Domain.Projections;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Application.Data;

public interface IApplicationDbContext
{
    DbSet<Account> Accounts { get; }
    DbSet<LedgerTransaction> LedgerTransactions { get; }
    DbSet<LedgerEntry> LedgerEntries { get; }
    Microsoft.EntityFrameworkCore.DbSet<LedgerCore.Domain.Projections.AccountBalance> AccountBalances { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
