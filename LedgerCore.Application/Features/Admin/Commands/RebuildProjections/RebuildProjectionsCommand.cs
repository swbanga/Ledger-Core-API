using MediatR;

namespace LedgerCore.Application.Features.Admin.Commands.RebuildProjections;

public record RebuildProjectionsResult(
    int RebuiltAccountsCount,
    int ProcessedTransactionsCount);

public class RebuildProjectionsCommand : IRequest<RebuildProjectionsResult>
{
}
