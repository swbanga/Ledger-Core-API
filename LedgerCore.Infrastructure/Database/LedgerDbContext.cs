using Microsoft.EntityFrameworkCore;
using LedgerCore.Application.Data;
using LedgerCore.Domain.Entities;

namespace LedgerCore.Infrastructure.Database;

public class LedgerDbContext : DbContext, IApplicationDbContext
{
    public LedgerDbContext(DbContextOptions<LedgerDbContext> options) : base(options)
    {
    }

    public DbSet<Account> Accounts { get; set; }
    public DbSet<LedgerTransaction> LedgerTransactions { get; set; }
    public DbSet<LedgerEntry> LedgerEntries { get; set; }
}
