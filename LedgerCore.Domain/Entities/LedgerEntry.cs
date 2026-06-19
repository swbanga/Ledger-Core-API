using System;
using LedgerCore.Domain.Enums;

namespace LedgerCore.Domain.Entities;

public class LedgerEntry
{
    public Guid Id { get; private set; }
    public Guid TransactionId { get; private set; }
    public Guid AccountId { get; private set; }
    public decimal Amount { get; private set; }
    public EntryDirection Direction { get; private set; }

    // Required by Entity Framework Core for materialization
    private LedgerEntry() { }

    public LedgerEntry(Guid id, Guid transactionId, Guid accountId, decimal amount, EntryDirection direction)
    {
        // Master Blueprint Rule 4: Forbidden 0.00 entries
        if (amount == 0)
            throw new ArgumentException("FATAL: Ledger entry amount cannot be zero.", nameof(amount));

        Id = id;
        TransactionId = transactionId;
        AccountId = accountId;
        Amount = amount; // Accepts the negative Debit from the Command Handler
        Direction = direction;
    }
}