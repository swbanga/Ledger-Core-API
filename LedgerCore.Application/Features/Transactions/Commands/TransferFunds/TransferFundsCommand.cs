using System;
using LedgerCore.Application.Behaviors; // Import the interface
using LedgerCore.Application.Contracts;

namespace LedgerCore.Application.Features.Transactions.Commands.TransferFunds;

public sealed record TransferFundsCommand(
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    string Currency,
    Guid IdempotencyKey) : IIdempotentCommand<Guid>, IRateLimitedCommand, IFinancialCommand
{
    public Guid RateLimitEntityId => SourceAccountId;
}
