using System;
using System.Collections.Generic;
using System.Linq;
using LedgerCore.Domain.Enums;
using LedgerCore.Domain.Exceptions;
using LedgerCore.Domain.ValueObjects;
using LedgerCore.Domain.Events;

namespace LedgerCore.Domain.Entities;

public sealed class LedgerTransaction
{
    public Guid Id { get; private set; }
    public string ReferenceCode { get; private set; }
    public TransactionStatus Status { get; private set; }
    public TransactionType Type { get; private set; }
    public CurrencyCode Currency { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    private readonly List<LedgerEntry> _entries = new();
    public IReadOnlyCollection<LedgerEntry> Entries => _entries.AsReadOnly();
    public AuditMetadata AuditMeta { get; private set; }

    private readonly List<IDomainEvent> _domainEvents = new();

    public void AddEntry(LedgerCore.Domain.Entities.LedgerEntry entry)
    {
        if (Status != LedgerCore.Domain.Enums.TransactionStatus.Pending)
            throw new System.InvalidOperationException("Cannot append entries to a finalized transaction.");

        _entries.Add(entry);
    }
    public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _domainEvents.ToList();
    public void ClearDomainEvents() => _domainEvents.Clear();
    private void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    private LedgerTransaction()
    {
        ReferenceCode = null!;
        Currency = null!;
        AuditMeta = null!;
    } // EF Core constructor

    public LedgerTransaction(
        Guid id,
        string referenceCode,
        TransactionType type,
        CurrencyCode currency,
        DateTimeOffset createdAt,
        IReadOnlyCollection<LedgerEntry> entries,
        AuditMetadata auditMeta)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty.", nameof(id));

        if (string.IsNullOrWhiteSpace(referenceCode))
            throw new ArgumentException("ReferenceCode cannot be null or whitespace.", nameof(referenceCode));

        if (entries == null)
            throw new ArgumentException("Entries collection cannot be null.", nameof(entries));

        if (auditMeta == null)
            throw new ArgumentNullException(nameof(auditMeta));

        Id = id;
        ReferenceCode = referenceCode;
        Status = TransactionStatus.Pending;
        Type = type;
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
        CreatedAt = createdAt;
        _entries.AddRange(entries);
        AuditMeta = auditMeta;
    }

    public void Post(TimeProvider timeProvider)
    {
        if (timeProvider == null)
            throw new ArgumentNullException(nameof(timeProvider));

        // Master Blueprint Rule 2: Balanced transaction: SUM(Entries) = 0
        // We enforce strict Signed Number mathematics. No dynamic sign flipping.
        var sum = Entries.Sum(e => e.Amount);

        if (sum != 0m)
        {
            throw new DomainInvariantViolationException(
                $"Transaction {Id} cannot be posted because the sum of its entries ({sum}) is not zero.");
        }

        // Extract the credit and debit amounts to broadcast the event
        var debitEntry = Entries.First(e => e.Direction == LedgerCore.Domain.Enums.EntryDirection.Debit);
        var creditEntry = Entries.First(e => e.Direction == LedgerCore.Domain.Enums.EntryDirection.Credit);

        RaiseDomainEvent(new FundsTransferredDomainEvent(
            Id,
            debitEntry.AccountId,
            creditEntry.AccountId,
            creditEntry.Amount,
            Currency.Value));

        Status = TransactionStatus.Posted;
    }
}
