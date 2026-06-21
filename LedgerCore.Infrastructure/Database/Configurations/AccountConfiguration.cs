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

        // --- BURNED CORE SYSTEM ACCOUNTS ---
        builder.HasData(
            new Account
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                AccountNumber = AccountNumber.CreateSystemAccount("0000000001"),
                AccountType = AccountType.Suspense,
                KycTier = KycTier.Tier4,
                Currency = "ZWL",
                Status = AccountStatus.Active,
                CreatedUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                RowVersion = new byte[8]
            },
            new Account
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                AccountNumber = AccountNumber.CreateSystemAccount("0000000002"),
                AccountType = AccountType.TaxLiability,
                KycTier = KycTier.Tier4,
                Currency = "ZWL",
                Status = AccountStatus.Active,
                CreatedUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                RowVersion = new byte[8]
            },
            new Account
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                AccountNumber = AccountNumber.CreateSystemAccount("0000000003"),
                AccountType = AccountType.SystemRevenue,
                KycTier = KycTier.Tier4,
                Currency = "ZWL",
                Status = AccountStatus.Active,
                CreatedUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                RowVersion = new byte[8]
            },
            new Account
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                AccountNumber = AccountNumber.CreateSystemAccount("0000000004"),
                AccountType = AccountType.Settlement,
                KycTier = KycTier.Tier4,
                Currency = "ZWL",
                Status = AccountStatus.Active,
                CreatedUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                RowVersion = new byte[8]
            },
            new Account
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                AccountNumber = AccountNumber.CreateSystemAccount("0000000005"),
                AccountType = AccountType.Reserve,
                KycTier = KycTier.Tier4,
                Currency = "ZWL",
                Status = AccountStatus.Active,
                CreatedUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                RowVersion = new byte[8]
            }
        );
    }
}
