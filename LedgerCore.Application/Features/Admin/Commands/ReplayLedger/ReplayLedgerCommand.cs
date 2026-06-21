using System;
using System.Collections.Generic;
using MediatR;

namespace LedgerCore.Application.Features.Admin.Commands.ReplayLedger;

public record ReplayResult(
    int TotalTransactionsProcessed,
    bool IsMathematicallyValid,
    List<Guid> CorruptedTransactionIds);

public class ReplayLedgerCommand : IRequest<ReplayResult>
{
}
