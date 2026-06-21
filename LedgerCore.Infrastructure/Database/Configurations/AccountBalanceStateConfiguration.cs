using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LedgerCore.Domain.ReadModels;

namespace LedgerCore.Infrastructure.Database.Configurations;

public sealed class AccountBalanceStateConfiguration : IEntityTypeConfiguration<AccountBalanceState>
{
    public void Configure(EntityTypeBuilder<AccountBalanceState> builder)
    {
        builder.ToTable("AccountBalanceStates");
        builder.HasKey(x => x.AccountId);
        builder.Property(x => x.CurrentBalance).HasPrecision(19, 4).IsRequired();
        builder.Property(x => x.LastTransactionId).IsRequired();
        builder.Property(x => x.LastUpdatedUtc).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
