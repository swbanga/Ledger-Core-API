using System;

namespace LedgerCore.Domain.ReadModels;

public class AccountBalanceState
{
    public Guid AccountId { get; set; }
    public decimal CurrentBalance { get; set; }
    public Guid LastTransactionId { get; set; }
    public DateTime LastUpdatedUtc { get; set; }
    public byte[] RowVersion { get; private set; } = null!;

    public AccountBalanceState()
    {
    }

    public AccountBalanceState(Guid accountId)
    {
        AccountId = accountId;
        CurrentBalance = 0;
        LastTransactionId = Guid.Empty;
        LastUpdatedUtc = DateTime.UtcNow;
    }
}
