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

    /// <summary>
    /// The identifier of the user who owns this account.
    /// </summary>
    public Guid OwnerUserId { get; set; }

    // Optimistic Concurrency Token (SQL Server RowVersion)
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    // Required for concurrency token interaction
    public DateTime LastActivityUtc { get; private set; }

    /// <summary>
    /// Marks the account as modified, forcing an UPDATE with WHERE RowVersion.
    /// </summary>
    public void MarkActivity()
    {
        LastActivityUtc = DateTime.UtcNow;
    }
}
