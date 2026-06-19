using System;
using LedgerCore.Domain.Enums;
using LedgerCore.Domain.ValueObjects;

namespace LedgerCore.Domain.Entities;

public class Account
{
    public Guid Id { get; set; }
    public AccountNumber AccountNumber { get; set; } = null!;
    public LedgerCore.Domain.Enums.AccountType AccountType { get; set; }
    public LedgerCore.Domain.Enums.KycTier KycTier { get; set; }

    // Keep the Entity Framework RowVersion for optimistic concurrency
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
