using System;
using LedgerCore.Application.Behaviors; // Import the interface

namespace LedgerCore.Application.Features.Transactions.Commands.TransferFunds;

public sealed record TransferFundsCommand(
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    string Currency,
    Guid IdempotencyKey) : IIdempotentCommand<Guid>; // Implement the Idempotency Lock