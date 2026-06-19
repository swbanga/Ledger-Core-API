using LedgerCore.Domain.Entities;
using LedgerCore.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerCore.Infrastructure.Database.Configurations;

public sealed class LedgerTransactionConfiguration : IEntityTypeConfiguration<LedgerTransaction>
{
    public void Configure(EntityTypeBuilder<LedgerTransaction> builder)
    {
        builder.ToTable("LedgerTransactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.ReferenceCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(t => t.ReferenceCode).IsUnique();

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.Currency)
            .HasConversion(
                currency => currency.Value,
                value => new CurrencyCode(value))
            .HasColumnName("Currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.OwnsOne(t => t.AuditMeta, audit =>
        {
            audit.Property(a => a.CreatedByUserId).HasColumnName("CreatedByUserId");
            audit.Property(a => a.IpAddress).HasColumnName("IpAddress").HasMaxLength(45);
            audit.Property(a => a.Channel).HasConversion<string>().HasColumnName("Channel").HasMaxLength(20);
        });

        builder.HasMany(t => t.Entries)
            .WithOne()
            .HasForeignKey(e => e.TransactionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}