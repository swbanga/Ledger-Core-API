using System;

namespace LedgerCore.Domain.Exceptions;

public class DomainInvariantViolationException : Exception
{
    public DomainInvariantViolationException()
    {
    }

    public DomainInvariantViolationException(string message) : base(message)
    {
    }

    public DomainInvariantViolationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
