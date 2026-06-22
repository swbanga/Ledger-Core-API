namespace LedgerCore.Application.Exceptions;

public class ConcurrencyException : System.Exception
{
    public ConcurrencyException(string message) : base(message) { }
    public ConcurrencyException(string message, System.Exception inner) : base(message, inner) { }
}
