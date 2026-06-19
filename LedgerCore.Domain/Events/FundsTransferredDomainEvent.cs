using System;

namespace LedgerCore.Domain.Events;

public sealed record FundsTransferredDomainEvent(
    Guid TransactionId,
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    string Currency) : IDomainEvent;
