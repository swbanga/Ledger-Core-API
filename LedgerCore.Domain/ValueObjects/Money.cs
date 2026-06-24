using System;

namespace LedgerCore.Domain.ValueObjects;

public sealed record Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Money amount must be non-negative.");

        Amount = amount;
        Currency = currency;
    }

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
        {
            throw new InvalidOperationException(
                $"FATAL: Currency mismatch. Cannot add {a.Currency} to {b.Currency}.");
        }

        return new Money(a.Amount + b.Amount, a.Currency);
    }
}