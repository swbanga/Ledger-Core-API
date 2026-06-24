using System;
using System.Collections.Generic;
using System.Linq;
using LedgerCore.Domain.Enums;
using LedgerCore.Domain.Events;
using LedgerCore.Domain.ValueObjects;

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

    public AuditMetadata AuditMetadata { get; private set; }

    public IReadOnlyCollection<LedgerEntry> Entries => _entries.AsReadOnly();

    public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    // Required by EF Core
    protected LedgerTransaction()
    {
        AuditMetadata = null!;
    }

    public LedgerTransaction(Guid id, string referenceCode, TransactionType type, string correlationId, AuditMetadata auditMetadata)
    {
        Id = id;
        ReferenceCode = referenceCode;
        TransactionType = type;
        Status = TransactionStatus.Pending;
        TimestampUtc = DateTimeOffset.UtcNow;
        CorrelationId = correlationId;
        AuditMetadata = auditMetadata;
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

        if (_entries.Count < 2)
            throw new InvalidOperationException("FATAL: Transaction must contain at least 2 entries.");

        if (_entries.Any(e => e.Value.Amount == 0))
            throw new InvalidOperationException("FATAL: Ghost entry detected. $0.00 entries are forbidden.");

        if (_entries.Select(e => e.Value.Currency).Distinct().Count() > 1)
            throw new InvalidOperationException("FATAL: Mixed-currency transaction matrix detected. All entries must resolve to a single currency.");

        var balance = _entries.Sum(e => e.Direction == Direction.Credit ? e.Value.Amount : -e.Value.Amount);
        if (balance != 0)
            throw new InvalidOperationException($"FATAL: Double-entry invariant violated. Imbalance of {balance}.");

        Status = TransactionStatus.Posted;
        
        _domainEvents.Add(new LedgerCore.Domain.Events.TransactionPostedDomainEvent(Id, _entries.AsReadOnly()));
    }
}
