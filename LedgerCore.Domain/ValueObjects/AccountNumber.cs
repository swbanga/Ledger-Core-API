using System;

namespace LedgerCore.Domain.ValueObjects;

public sealed record AccountNumber
{
    public string Value { get; }

    public AccountNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Account number cannot be null or whitespace.", nameof(value));

        if (value.Length != 10)
            throw new ArgumentException("Account number must be exactly 10 characters.", nameof(value));

        if (value[0] != '0')
            throw new ArgumentException("Account number must start with '0'.", nameof(value));

        if (!IsAllDigits(value))
            throw new ArgumentException("Account number must consist only of digits.", nameof(value));

        Value = value;
    }

    private static bool IsAllDigits(string s)
    {
        foreach (char c in s)
        {
            if (!char.IsDigit(c))
                return false;
        }
        return true;
    }

    public static implicit operator string(AccountNumber accountNumber) => accountNumber.Value;

    public override string ToString() => Value;
}
