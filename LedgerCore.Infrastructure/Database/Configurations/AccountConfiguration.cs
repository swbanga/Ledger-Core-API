using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LedgerCore.Domain.Entities;
using LedgerCore.Domain.Enums;
using LedgerCore.Domain.ValueObjects;

namespace LedgerCore.Infrastructure.Database.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AccountNumber)
            .HasConversion(
                v => v.Value,
                v => AccountNumber.CreateSystemAccount(v))
            .HasMaxLength(20)
            .IsRequired();

        // Mathematically forces Enums to store as strings
        builder.Property(x => x.AccountType).HasConversion<string>();
        builder.Property(x => x.KycTier).HasConversion<string>();

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        // --- PHASE 1, STAGE 2: CORE SEEDING (COLLISION-FREE GUIDS) ---
        builder.HasData(
            new Account
            {
                Id = Guid.Parse("F0000000-0000-0000-0000-000000000001"),
                AccountNumber = AccountNumber.CreateSystemAccount("SYS-REVENUE"),
                AccountType = AccountType.SystemRevenue,
                KycTier = KycTier.Tier4
            },
            new Account
            {
                Id = Guid.Parse("F0000000-0000-0000-0000-000000000002"),
                AccountNumber = AccountNumber.CreateSystemAccount("SYS-TAX-ZIMRA"),
                AccountType = AccountType.TaxLiability,
                KycTier = KycTier.Tier4
            },
            new Account
            {
                Id = Guid.Parse("F0000000-0000-0000-0000-000000000003"),
                AccountNumber = AccountNumber.CreateSystemAccount("SYS-SETTLEMENT"),
                AccountType = AccountType.Settlement,
                KycTier = KycTier.Tier4
            },
            new Account
            {
                Id = Guid.Parse("F0000000-0000-0000-0000-000000000004"),
                AccountNumber = AccountNumber.CreateSystemAccount("SYS-SUSPENSE"),
                AccountType = AccountType.Suspense,
                KycTier = KycTier.Tier4
            }
        );
    }
}
