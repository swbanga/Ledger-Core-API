using LedgerCore.Application.Data;
using LedgerCore.Domain.Entities;
using LedgerCore.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Account> Accounts { get; set; }
    public DbSet<LedgerTransaction> LedgerTransactions { get; set; }
    public DbSet<LedgerEntry> LedgerEntries { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }
}
