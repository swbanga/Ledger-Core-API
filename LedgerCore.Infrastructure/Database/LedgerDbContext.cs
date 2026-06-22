using Microsoft.EntityFrameworkCore;
using LedgerCore.Application.Contracts;
using LedgerCore.Domain.Entities;
using LedgerCore.Domain.Projections;
using LedgerCore.Domain.ReadModels;
using LedgerCore.Infrastructure.Outbox;

namespace LedgerCore.Infrastructure.Database;

public class LedgerDbContext : DbContext, IApplicationDbContext
{
    public LedgerDbContext(DbContextOptions<LedgerDbContext> options) : base(options)
    {
    }

    public DbSet<Account> Accounts { get; set; }
    public DbSet<LedgerTransaction> LedgerTransactions { get; set; }
    public DbSet<LedgerEntry> LedgerEntries { get; set; }
    public DbSet<AccountBalance> AccountBalances { get; set; }
    public DbSet<AccountBalanceState> AccountBalanceStates { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LedgerDbContext).Assembly);

        // --- THE OUTBOX ORM WELD ---
        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.ToTable("OutboxMessages");
            b.HasKey(x => x.Id);
        });

        // --- MONEY VALUE OBJECT EF CORE MAPPING ---
        modelBuilder.Entity<LedgerCore.Domain.Entities.LedgerEntry>(b =>
        {
            b.OwnsOne(e => e.Value, m =>
            {
                m.Property(p => p.Amount).HasPrecision(19, 4).HasColumnName("Amount");
                m.Property(p => p.Currency).HasMaxLength(3).HasColumnName("Currency");
            });
        });
        
        // --- PHASE 3: CQRS READ MODEL SNAPSHOT ---
        modelBuilder.Entity<AccountBalance>(b =>
    {
        b.ToTable("AccountBalances");
        b.HasKey(x => x.AccountId); 

        b.Property(x => x.CurrentBalance)
         .HasPrecision(19, 4)
         .IsRequired();

        b.Property(x => x.RowVersion).IsRowVersion();
    });
    }
}
