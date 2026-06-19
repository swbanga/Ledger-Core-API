using System;

namespace LedgerCore.Application.Exceptions;

public class IdempotencyException : Exception
{
    public IdempotencyException(string message) : base(message) { }
}
