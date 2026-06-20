using System;

namespace LedgerCore.Domain.Projections;

public class AccountBalance
{
    public Guid AccountId { get; set; }
    public decimal CurrentBalance { get; set; }
    public DateTimeOffset LastUpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
