using System;

namespace LedgerCore.Application.Contracts;

public interface IRateLimitedCommand
{
    Guid RateLimitEntityId { get; }
}
