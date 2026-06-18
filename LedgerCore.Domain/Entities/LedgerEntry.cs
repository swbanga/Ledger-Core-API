using System;
using LedgerCore.Domain.Enums;

namespace LedgerCore.Domain.Entities;

public sealed class LedgerEntry
{
    public Guid Id { get; private set; }
    public Guid TransactionId { get; private set; }
    public Guid AccountId { get; private set; }
    public decimal Amount { get; private set; }
    public EntryDirection Direction { get; private set; }

    private LedgerEntry() { } // EF Core constructor

    public LedgerEntry(Guid id, Guid transactionId, Guid accountId, decimal amount, EntryDirection direction)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty.", nameof(id));

        if (transactionId == Guid.Empty)
            throw new ArgumentException("TransactionId cannot be empty.", nameof(transactionId));

        if (accountId == Guid.Empty)
            throw new ArgumentException("AccountId cannot be empty.", nameof(accountId));

        if (amount <= 0)
            throw new ArgumentException("Amount must be positive.", nameof(amount));

        Id = id;
        TransactionId = transactionId;
        AccountId = accountId;
        Amount = amount;
        Direction = direction;
    }
}
