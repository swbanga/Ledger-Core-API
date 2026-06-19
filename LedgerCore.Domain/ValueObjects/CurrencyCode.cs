using System;
using System.Text.RegularExpressions;

namespace LedgerCore.Domain.ValueObjects;

public sealed record CurrencyCode
{
    public string Value { get; init; }

    public CurrencyCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Currency code cannot be empty.");

        if (!Regex.IsMatch(value, @"^[A-Za-z]{3}$"))
            throw new ArgumentException("Currency code must be exactly 3 letters.");

        Value = value.ToUpperInvariant();
    }

    public static implicit operator string(CurrencyCode code) => code?.Value ?? string.Empty;
}
