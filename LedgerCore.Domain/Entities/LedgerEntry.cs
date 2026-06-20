using System;
using LedgerCore.Domain.Enums;
using LedgerCore.Domain.ValueObjects;

namespace LedgerCore.Domain.Entities;

public class LedgerEntry
{
    public Guid Id { get; private set; }
    public Guid TransactionId { get; private set; }
    public Guid AccountId { get; private set; }
    public Money Value { get; private set; }
    public EntryDirection Direction { get; private set; }

    // Required by Entity Framework Core for materialization
    private LedgerEntry() { }

    public LedgerEntry(Guid id, Guid transactionId, Guid accountId, Money value, EntryDirection direction)
    {
        // Master Blueprint Rule 4: Forbidden 0.00 entries
        if (value.Amount == 0)
            throw new ArgumentException("FATAL: Ledger entry amount cannot be zero.", nameof(value));

        Id = id;
        TransactionId = transactionId;
        AccountId = accountId;
        Value = value;
        Direction = direction;
    }
}
