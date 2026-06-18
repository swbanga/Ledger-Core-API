using System;

namespace LedgerCore.Domain.ValueObjects;

public sealed record CurrencyCode
{
    public string Value { get; }

    public CurrencyCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Currency code cannot be null or whitespace.", nameof(value));

        if (value.Length != 3)
            throw new ArgumentException("Currency code must be exactly 3 characters.", nameof(value));

        if (!IsAllLetters(value))
            throw new ArgumentException("Currency code must consist only of letters.", nameof(value));

        Value = value.ToUpperInvariant();
    }

    private static bool IsAllLetters(string s)
    {
        foreach (char c in s)
        {
            if (!char.IsLetter(c))
                return false;
        }
        return true;
    }

    public static implicit operator string(CurrencyCode currencyCode) => currencyCode.Value;

    public override string ToString() => Value;
}
