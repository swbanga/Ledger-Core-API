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

        builder.Property(t => t.TransactionType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();


        builder.HasMany(t => t.Entries)
            .WithOne()
            .HasForeignKey(e => e.TransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(t => t.AuditMetadata, audit =>
        {
            audit.Property(a => a.UserId)
                .HasColumnName("Audit_UserId")
                .IsRequired();
            audit.Property(a => a.IpAddress)
                .HasColumnName("Audit_IpAddress")
                .HasMaxLength(50)
                .IsRequired(false);
            audit.Property(a => a.DeviceId)
                .HasColumnName("Audit_DeviceId")
                .HasMaxLength(50)
                .IsRequired(false);
        });
    }
}
