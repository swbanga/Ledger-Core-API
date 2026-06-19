using System;
using System.Text.RegularExpressions;

namespace LedgerCore.Domain.ValueObjects;

public sealed record AccountNumber
{
    public string Value { get; }

    private AccountNumber(string value)
    {
        Value = value;
    }

    public static AccountNumber CreateUserAccount(string number)
    {
        if (string.IsNullOrWhiteSpace(number) || !Regex.IsMatch(number, @"^0\d{9}$"))
            throw new ArgumentException("User account numbers must be exactly 10 digits and start with 0.");
       
        return new AccountNumber(number);
    }

    public static AccountNumber CreateAgentOrMerchantAccount(string number)
    {
        if (string.IsNullOrWhiteSpace(number) || !Regex.IsMatch(number, @"^\d{6}$"))
            throw new ArgumentException("Agent/Merchant account numbers must be exactly 6 digits.");
       
        return new AccountNumber(number);
    }

    public static AccountNumber CreateSystemAccount(string number)
    {
        if (string.IsNullOrWhiteSpace(number) || number.Length < 3)
            throw new ArgumentException("System accounts require a specific internal identifier.");
       
        return new AccountNumber(number);
    }

    // Implicit conversion to string for database and JSON serialization
    public static implicit operator string(AccountNumber accountNumber) => accountNumber.Value;
}
