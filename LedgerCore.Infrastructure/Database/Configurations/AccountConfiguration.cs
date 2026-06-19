using LedgerCore.Domain.Entities;
using LedgerCore.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerCore.Infrastructure.Database.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AccountNumber)
            .HasConversion(
                accountNumber => accountNumber.Value,
                value => new AccountNumber(value))
            .HasColumnName("AccountNumber")
            .HasMaxLength(10)
            .IsRequired();

        builder.HasIndex(a => a.AccountNumber).IsUnique();

        builder.Property(a => a.AccountType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.KycTier)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasMany<LedgerEntry>()
            .WithOne()
            .HasForeignKey(le => le.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}