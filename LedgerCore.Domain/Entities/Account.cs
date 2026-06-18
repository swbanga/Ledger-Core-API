using System;
using LedgerCore.Domain.Enums;
using LedgerCore.Domain.ValueObjects;

namespace LedgerCore.Domain.Entities;

public sealed class Account
{
    public Guid Id { get; private set; }
    public AccountNumber AccountNumber { get; private set; }
    public AccountType AccountType { get; private set; }
    public KycTier KycTier { get; private set; }
    public byte[] RowVersion { get; private set; }

    private Account() { } // EF Core constructor

    public Account(Guid id, AccountNumber accountNumber, AccountType accountType, KycTier kycTier)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty.", nameof(id));

        Id = id;
        AccountNumber = accountNumber ?? throw new ArgumentNullException(nameof(accountNumber));
        AccountType = accountType;
        KycTier = kycTier;
        RowVersion = Array.Empty<byte>();
    }
}
