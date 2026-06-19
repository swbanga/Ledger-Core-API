using LedgerCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Application.Data;

public interface IApplicationDbContext
{
    DbSet<Account> Accounts { get; }
    DbSet<LedgerTransaction> LedgerTransactions { get; }
    DbSet<LedgerEntry> LedgerEntries { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
