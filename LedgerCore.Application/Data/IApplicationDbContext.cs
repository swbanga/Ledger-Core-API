using LedgerCore.Domain.Entities;
using LedgerCore.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Application.Data;

public interface IApplicationDbContext
{
    DbSet<Account> Accounts { get; }
    DbSet<LedgerTransaction> LedgerTransactions { get; }
    DbSet<LedgerEntry> LedgerEntries { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
