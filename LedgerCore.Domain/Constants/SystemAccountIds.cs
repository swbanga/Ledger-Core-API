using System;

namespace LedgerCore.Domain.Constants;

public static class SystemAccountIds
{
    public static readonly Guid SystemRevenue = Guid.Parse("F0000000-0000-0000-0000-000000000001");
    public static readonly Guid TaxLiabilityZimra = Guid.Parse("F0000000-0000-0000-0000-000000000002");
    public static readonly Guid Settlement = Guid.Parse("F0000000-0000-0000-0000-000000000003");
    public static readonly Guid Suspense = Guid.Parse("F0000000-0000-0000-0000-000000000004");
    public static readonly Guid SystemReserve = Guid.Parse("F0000000-0000-0000-0000-000000000005");
}
