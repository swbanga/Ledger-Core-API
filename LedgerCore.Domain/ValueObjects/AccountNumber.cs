using System;
using System.Text.RegularExpressions;

namespace LedgerCore.Domain.ValueObjects;

public sealed record AccountNumber
{
    public string Value { get; init; }

    public AccountNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Account number cannot be empty.");

        if (!Regex.IsMatch(value, @"^0\d{9}$"))
            throw new ArgumentException("Account number must be exactly 10 digits and start with 0.");

        Value = value;
    }

    public static implicit operator string(AccountNumber number) => number?.Value ?? string.Empty;
}
