using System;
using System.Collections.Generic;
using System.Linq;
using LedgerCore.Domain.Enums;
using LedgerCore.Domain.Events;

namespace LedgerCore.Domain.Entities;

public class LedgerTransaction
{
    private readonly List<LedgerEntry> _entries = new();
    private readonly List<IDomainEvent> _domainEvents = new();

    public Guid Id { get; private set; }
    public string ReferenceCode { get; private set; } = string.Empty;
    public TransactionType TransactionType { get; private set; }
    public TransactionStatus Status { get; private set; }
    public DateTimeOffset TimestampUtc { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;

    public IReadOnlyCollection<LedgerEntry> Entries => _entries.AsReadOnly();

    public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    // Required by EF Core
    protected LedgerTransaction() { }

    public LedgerTransaction(Guid id, string referenceCode, TransactionType type, string correlationId)
    {
        Id = id;
        ReferenceCode = referenceCode;
        TransactionType = type;
        Status = TransactionStatus.Pending;
        TimestampUtc = DateTimeOffset.UtcNow;
        CorrelationId = correlationId;
    }

    public void AddEntry(LedgerEntry entry)
    {
        if (Status != TransactionStatus.Pending)
            throw new InvalidOperationException("Cannot append entries to a finalized transaction.");
            
        _entries.Add(entry);
    }

    // THE ABSOLUTE MATHEMATICAL INVARIANT
    public void Post()
    {
        if (Status != TransactionStatus.Pending)
            throw new InvalidOperationException("Only pending transactions can be posted.");

        var balance = _entries.Sum(e => e.Amount);
        if (balance != 0)
            throw new InvalidOperationException($"FATAL: Double-entry invariant violated. Imbalance of {balance}. Logical money creation/destruction detected.");

        Status = TransactionStatus.Posted;
    }
}
