using System;

namespace LedgerCore.Domain.ValueObjects;

public sealed record Money(decimal Amount, string Currency)
{
    public static Money operator +(Money a, Money b) =>
        a.Currency == b.Currency ? new Money(a.Amount + b.Amount, a.Currency) : throw new InvalidOperationException($"FATAL: Currency mismatch. Cannot add {a.Currency} to {b.Currency}.");

    public static Money operator -(Money a, Money b) =>
        a.Currency == b.Currency ? new Money(a.Amount - b.Amount, a.Currency) : throw new InvalidOperationException($"FATAL: Currency mismatch. Cannot subtract {b.Currency} from {a.Currency}.");
}
