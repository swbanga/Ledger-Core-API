using System;
using System.Linq;
using Xunit;
using LedgerCore.Domain.Entities;
using LedgerCore.Domain.Enums;
using LedgerCore.Domain.ValueObjects;

namespace LedgerCore.IntegrationTests;

public class LedgerConsistencyTests
{
    private static AuditMetadata GenerateAudit()
        => new AuditMetadata(Guid.NewGuid(), "127.0.0.1", "device");

    // ---------- double‑entry balancing / sum zero ----------

    [Fact]
    public void Post_WithValidEntries_SetsStatusToPosted_AndSumIsZero()
    {
        var txId = Guid.NewGuid();
        var tx = new LedgerTransaction(
            txId,
            "REF-001",
            TransactionType.PeerToPeer,
            "corr-1",
            GenerateAudit());

        var debit = new LedgerEntry(Guid.NewGuid(), txId, Guid.NewGuid(), new Money(150m, "USD"), EntryDirection.Debit);
        var credit = new LedgerEntry(Guid.NewGuid(), txId, Guid.NewGuid(), new Money(150m, "USD"), EntryDirection.Credit);
        tx.AddEntry(debit);
        tx.AddEntry(credit);

        tx.Post();

        Assert.Equal(TransactionStatus.Posted, tx.Status);
        Assert.Equal(0, tx.Entries.Sum(e => e.Value.Amount));
    }


    // ---------- minimum entry count ----------


    // ---------- zero‑value rejection ----------

    [Fact]
    public void LedgerEntry_WithZeroAmount_ThrowsArgument()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => new LedgerEntry(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new Money(0m, "USD"), EntryDirection.Credit));

        Assert.Contains("zero", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ---------- currency consistency ----------

    [Fact]
    public void Post_WithMixedCurrency_ThrowsInvalidOperationException()
    {
        var tx = new LedgerTransaction(
            Guid.NewGuid(),
            "REF-003",
            TransactionType.PeerToPeer,
            "corr-3",
            GenerateAudit());

        var e1 = new LedgerEntry(Guid.NewGuid(), tx.Id, Guid.NewGuid(), new Money(100m, "USD"), EntryDirection.Credit);
        var e2 = new LedgerEntry(Guid.NewGuid(), tx.Id, Guid.NewGuid(), new Money(50m, "USD"), EntryDirection.Debit);
        var e3 = new LedgerEntry(Guid.NewGuid(), tx.Id, Guid.NewGuid(), new Money(50m, "EUR"), EntryDirection.Debit);
        tx.AddEntry(e1);
        tx.AddEntry(e2);
        tx.AddEntry(e3);

        var ex = Assert.Throws<InvalidOperationException>(() => tx.Post());
        Assert.Contains("Mixed-currency", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ---------- already posted guard ----------

    [Fact]
    public void Post_WhenAlreadyPosted_ThrowsInvalidOperationException()
    {
        var tx = new LedgerTransaction(
            Guid.NewGuid(),
            "REF-005",
            TransactionType.PeerToPeer,
            "corr-5",
            GenerateAudit());
        var e1 = new LedgerEntry(Guid.NewGuid(), tx.Id, Guid.NewGuid(), new Money(50m, "USD"), EntryDirection.Debit);
        var e2 = new LedgerEntry(Guid.NewGuid(), tx.Id, Guid.NewGuid(), new Money(50m, "USD"), EntryDirection.Credit);
        tx.AddEntry(e1);
        tx.AddEntry(e2);
        tx.Post();

        var ex = Assert.Throws<InvalidOperationException>(() => tx.Post());
        Assert.Contains("posted", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ---------- add entry after finalized ----------

    [Fact]
    public void AddEntry_AfterPost_ThrowsInvalidOperationException()
    {
        var tx = new LedgerTransaction(
            Guid.NewGuid(),
            "REF-006",
            TransactionType.PeerToPeer,
            "corr-6",
            GenerateAudit());

        var e1 = new LedgerEntry(Guid.NewGuid(), tx.Id, Guid.NewGuid(), new Money(10m, "USD"), EntryDirection.Debit);
        var e2 = new LedgerEntry(Guid.NewGuid(), tx.Id, Guid.NewGuid(), new Money(10m, "USD"), EntryDirection.Credit);
        tx.AddEntry(e1);
        tx.AddEntry(e2);
        tx.Post();

        var ex = Assert.Throws<InvalidOperationException>(
            () => tx.AddEntry(new LedgerEntry(Guid.NewGuid(), tx.Id, Guid.NewGuid(), new Money(5m, "USD"), EntryDirection.Credit)));
        Assert.Contains("finalized", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
