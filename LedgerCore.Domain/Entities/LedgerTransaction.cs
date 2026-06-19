using System;
using System.Collections.Generic;
using System.Linq;
using LedgerCore.Domain.Enums;
using LedgerCore.Domain.Exceptions;
using LedgerCore.Domain.ValueObjects;

namespace LedgerCore.Domain.Entities;

public sealed class LedgerTransaction
{
    public Guid Id { get; private set; }
    public string ReferenceCode { get; private set; }
    public TransactionStatus Status { get; private set; }
    public TransactionType Type { get; private set; }
    public CurrencyCode Currency { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyCollection<LedgerEntry> Entries { get; private set; }
    public AuditMetadata AuditMeta { get; private set; }

    private LedgerTransaction()
    {
        ReferenceCode = null!;
        Currency = null!;
        Entries = null!;
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

        if (entries == null || entries.Count == 0)
            throw new ArgumentException("Entries collection cannot be null or empty.", nameof(entries));

        if (auditMeta == null)
            throw new ArgumentNullException(nameof(auditMeta));

        Id = id;
        ReferenceCode = referenceCode;
        Status = TransactionStatus.Pending;
        Type = type;
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
        CreatedAt = createdAt;
        Entries = entries;
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

        Status = TransactionStatus.Posted;
    }
}