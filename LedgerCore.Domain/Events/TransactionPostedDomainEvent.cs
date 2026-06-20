using System;
using System.Collections.Generic;
using LedgerCore.Domain.Entities;

namespace LedgerCore.Domain.Events;

// Assuming IDomainEvent is the interface you use for Outbox events
public sealed record TransactionPostedDomainEvent(Guid TransactionId, IReadOnlyCollection<LedgerEntry> Entries) : IDomainEvent;
