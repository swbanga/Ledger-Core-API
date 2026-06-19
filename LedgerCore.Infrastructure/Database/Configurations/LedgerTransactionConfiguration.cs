using LedgerCore.Domain.Entities;
using LedgerCore.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class LedgerTransactionConfiguration : IEntityTypeConfiguration<LedgerTransaction>
{
    public void Configure(EntityTypeBuilder<LedgerTransaction> builder)
    {
        builder.ToTable("LedgerTransactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Currency)
            .HasConversion(
                currency => currency.Value,
                value => new CurrencyCode(value))
            .HasColumnName("Currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.OwnsOne(t => t.AuditMeta);
    }
}
