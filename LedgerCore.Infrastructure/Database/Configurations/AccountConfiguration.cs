using LedgerCore.Domain.Entities;
using LedgerCore.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
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
    }
}
